using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmTool.Api.Models;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public IntegrationsController(IConfiguration configuration) => _configuration = configuration;

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
}
