using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PmTool.Api.Models;
using System.Net.Http.Json;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public IntegrationsController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("graph/status")]
    public ActionResult<GraphIntegrationStatusDto> GetGraphStatus()
    {
        var clientId = _configuration["MicrosoftGraph:ClientId"] ?? "";
        var tenantId = _configuration["MicrosoftGraph:TenantId"] ?? "";
        var redirectUri = _configuration["MicrosoftGraph:RedirectUri"] ?? "";
        var scopes = (_configuration["MicrosoftGraph:Scopes"] ?? "User.Read,Team.ReadBasic.All,Channel.ReadBasic.All,OnlineMeetings.Read")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var configured = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(redirectUri);

        return Ok(new GraphIntegrationStatusDto(
            configured,
            clientId,
            tenantId,
            redirectUri,
            scopes,
            configured
                ? "Microsoft Graph ist konfiguriert. Als naechstes kann OAuth-Login und Teams-Sync verdrahtet werden."
                : "ClientId, TenantId und RedirectUri in appsettings oder Environment Variablen setzen, um die echte Graph-Anbindung zu aktivieren."
        ));
    }

    [HttpGet("jira/status")]
    public ActionResult<JiraIntegrationStatusDto> GetJiraStatus()
    {
        var baseUrl = (_configuration["Jira:BaseUrl"] ?? "").Trim().TrimEnd('/');
        var email = (_configuration["Jira:Email"] ?? "").Trim();
        var authMode = string.IsNullOrWhiteSpace(_configuration["Jira:AuthMode"]) ? "basic" : _configuration["Jira:AuthMode"]!.Trim().ToLowerInvariant();
        var apiToken = (_configuration["Jira:ApiToken"] ?? "").Trim();

        var configured = !string.IsNullOrWhiteSpace(baseUrl)
            && !string.IsNullOrWhiteSpace(apiToken)
            && (authMode == "bearer" || !string.IsNullOrWhiteSpace(email));

        return Ok(new JiraIntegrationStatusDto(
            configured,
            baseUrl,
            authMode,
            email,
            configured
                ? "Jira ist vorbereitet. Als naechstes koennen Projektschluessel und Boards pro Projekt verknuepft werden."
                : "BaseUrl, ApiToken und bei Basic-Auth zusaetzlich Email in der Backend-Konfiguration setzen, um Jira-Tickets laden zu koennen."
        ));
    }

    [HttpGet("graph/auth/start")]
    public ActionResult<GraphAuthStartResponse> GetGraphAuthStart()
    {
        var clientId = _configuration["MicrosoftGraph:ClientId"] ?? "";
        var tenantId = _configuration["MicrosoftGraph:TenantId"] ?? "";
        var redirectUri = _configuration["MicrosoftGraph:RedirectUri"] ?? "";
        var scopes = GetGraphScopes();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest("Microsoft Graph ist noch nicht vollstaendig konfiguriert.");
        }

        var state = Guid.NewGuid().ToString("N");
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = state
        };

        var authorizationUrl = QueryHelpers.AddQueryString(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            query!);

        return Ok(new GraphAuthStartResponse(authorizationUrl, state, redirectUri));
    }

    [AllowAnonymous]
    [HttpGet("graph/auth/callback")]
    public async Task<ActionResult<GraphAuthCallbackResponse>> HandleGraphAuthCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(new GraphAuthCallbackResponse(false, $"Graph OAuth wurde abgebrochen: {error}", "", 0, ""));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new GraphAuthCallbackResponse(false, "Kein Authorization Code in der Callback-URL erhalten.", "", 0, ""));
        }

        var clientId = _configuration["MicrosoftGraph:ClientId"] ?? "";
        var tenantId = _configuration["MicrosoftGraph:TenantId"] ?? "";
        var clientSecret = _configuration["MicrosoftGraph:ClientSecret"] ?? "";
        var redirectUri = _configuration["MicrosoftGraph:RedirectUri"] ?? "";
        var scopes = GetGraphScopes();

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new GraphAuthCallbackResponse(false, "Graph-Konfiguration ist unvollstaendig. ClientId, TenantId, ClientSecret und RedirectUri werden benoetigt.", "", 0, ""));
        }

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["scope"] = string.Join(' ', scopes)
            })
        };

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return BadRequest(new GraphAuthCallbackResponse(false, $"Token-Austausch fehlgeschlagen: {errorBody}", "", 0, ""));
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GraphTokenResponse>();
        if (tokenResponse is null)
        {
            return BadRequest(new GraphAuthCallbackResponse(false, "Graph OAuth Antwort konnte nicht gelesen werden.", "", 0, ""));
        }

        return Ok(new GraphAuthCallbackResponse(
            true,
            "Graph OAuth erfolgreich abgeschlossen. Der naechste Schritt ist das sichere Persistieren und Nutzen des Tokens fuer Teams-Sync.",
            tokenResponse.Scope ?? string.Join(' ', scopes),
            tokenResponse.ExpiresIn,
            tokenResponse.TokenType ?? "Bearer"
        ));
    }

    private List<string> GetGraphScopes() =>
        (_configuration["MicrosoftGraph:Scopes"] ?? "openid,profile,offline_access,User.Read,Team.ReadBasic.All,Channel.ReadBasic.All,OnlineMeetings.Read")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private sealed record GraphTokenResponse(string? TokenType, int ExpiresIn, string? Scope);
}
