using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/debug")]
[AllowAnonymous]
public class DebugController : ControllerBase
{
    [HttpGet("error")]
    public IActionResult LanzarError() =>
        throw new InvalidOperationException(
            "Fallo simulado al abrir /var/app/data/techsupport.db (tabla Incidentes, SQLite)");
}
