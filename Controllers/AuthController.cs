using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PmTool.Api.Models;
using PmTool.Api.Security;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(AppDbContext db, IConfiguration cfg, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _cfg = cfg;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "E-Mail oder Passwort falsch." });

        return Ok(BuildLoginResponse(user));
    }

    [HttpPost("entra/exchange")]
    public async Task<ActionResult<LoginResponse>> ExchangeEntraToken([FromBody] EntraExchangeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.IdToken))
            return BadRequest(new { message = "Kein Entra ID Token uebergeben." });

        var tenantId = _cfg["EntraId:TenantId"] ?? _cfg["MicrosoftGraph:TenantId"] ?? "";
        var clientId = _cfg["EntraId:ClientId"] ?? _cfg["MicrosoftGraph:ClientId"] ?? "";
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
            return BadRequest(new { message = "Entra ID ist serverseitig nicht konfiguriert." });

        ClaimsPrincipal principal;
        try
        {
            principal = await ValidateEntraTokenAsync(req.IdToken, tenantId, clientId);
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { message = $"Entra ID Token ungueltig: {ex.Message}" });
        }

        var email = principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue(ClaimTypes.Upn)
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email))
            return Unauthorized(new { message = "Im Entra ID Token wurde keine E-Mail gefunden." });

        var name = principal.FindFirstValue("name") ?? email;

        var allowedDomains = (_cfg["EntraId:AllowedDomains"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (allowedDomains.Length > 0)
        {
            var domain = email.Split('@').LastOrDefault() ?? "";
            if (!allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                return Unauthorized(new { message = "Die Entra ID E-Mail-Domain ist nicht freigegeben." });
        }

        var user = await _db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            var autoProvision = bool.TryParse(_cfg["EntraId:AutoProvisionUsers"], out var enabled) && enabled;
            if (!autoProvision)
                return Unauthorized(new { message = "Der Benutzer ist im PM Tool nicht angelegt." });

            var defaultTenantSlug = _cfg["EntraId:DefaultTenantSlug"] ?? "realcore";
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == defaultTenantSlug)
                ?? await _db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();

            if (tenant == null)
                return BadRequest(new { message = "Es ist kein Tenant fuer die automatische Benutzeranlage vorhanden." });

            user = new User
            {
                DisplayName = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                Role = _cfg["EntraId:DefaultRole"] ?? "Member",
                TenantId = tenant.Id,
                Tenant = tenant,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (!string.Equals(user.DisplayName, name, StringComparison.Ordinal))
        {
            user.DisplayName = name;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(BuildLoginResponse(user));
    }

    [Authorize]
    [HttpGet("access-matrix")]
    public ActionResult<AccessMatrixDto> GetAccessMatrix()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        return Ok(new AccessMatrixDto(
            role,
            RoleCatalog.AvailableRoles.ToList(),
            RoleCatalog.BuildPermissionMatrix(role)
        ));
    }

    private LoginResponse BuildLoginResponse(User user)
    {
        var secret = _cfg["Jwt:Secret"] ?? "realcore-pm-secret-2026-heinemann-secure!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("tenantId", user.TenantId.ToString()),
        };

        var token = new JwtSecurityToken(
            "pmtool",
            "pmtool",
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.DisplayName,
            user.Email,
            user.Role,
            user.Id,
            user.TenantId);
    }

    private async Task<ClaimsPrincipal> ValidateEntraTokenAsync(string idToken, string tenantId, string clientId)
    {
        var handler = new JwtSecurityTokenHandler();
        var client = _httpClientFactory.CreateClient();
        var jwksJson = await client.GetStringAsync($"https://login.microsoftonline.com/{tenantId}/discovery/v2.0/keys");
        var jwks = new JsonWebKeySet(jwksJson);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/"
            },
            ValidateAudience = true,
            ValidAudiences = new[] { clientId },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "name",
            RoleClaimType = "roles",
        };

        return handler.ValidateToken(idToken, validationParameters, out _);
    }
}
