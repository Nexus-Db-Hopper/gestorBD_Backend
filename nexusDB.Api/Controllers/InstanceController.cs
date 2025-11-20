using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Instances;

namespace nexusDB.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InstanceController : ControllerBase
{
    private readonly IInstanceService _instanceService;
    public InstanceController(IInstanceService instanceService)
    {
        _instanceService = instanceService;
    }

    // Aqui es como se crea la instancia (para otras implementaciones investigar) se necesita rol de admin para usarlo
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateInstance([FromBody] CreateInstanceRequest request)
    {
        try
        {
            // Aqui se llama al servicio que se encuentra en application para crear la instancia
            var instanceId = await _instanceService.CreateInstanceAsync(request);
            return Ok(instanceId);
        }
        catch (Exception e)
        {
            // Este return es por si algo falla para ver el error, se puede trabajar mas el catch para errores especificos
            return BadRequest(e.Message);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ExecuteQueryAsync([FromBody] QueryRequestDto queryRequest)
    {
        try
        {
            var queryResult = await _instanceService.ExecuteQueryAsync(queryRequest);
            return Ok(queryResult);
        }
        catch (Exception e)
        {
            return BadRequest($"Unexpected error: {e.Message}");
        }
    }
}