using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Instances;

namespace nexusDB.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/instances")]
public class InstanceController : ControllerBase
{
    private readonly IInstanceService _instanceService;

    public InstanceController(IInstanceService instanceService)
    {
        _instanceService = instanceService;
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateInstance([FromBody] CreateInstanceRequest request)
    {
        try
        {
            var creatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(creatorId))
            {
                return Unauthorized();
            }
            request.CreatedByUserId = int.Parse(creatorId);

            // --- REFACTORING FOR SHARED ENVIRONMENT (UPS) ---
            // Generate a unique and predictable container name to prevent collisions.
            // This ignores any 'containerName' value sent by the client.
            // Format: nexusdb-app-[ownerUserId]-[sanitized-db-name]
            var sanitizedDbName = Regex.Replace(request.Name.ToLower(), "[^a-z0-9-]", "");
            var uniqueContainerName = $"nexusdb-app-{request.OwnerUserId}-{sanitizedDbName}";
            request.ContainerName = uniqueContainerName;
            // --- END OF REFACTORING ---

            var instanceId = await _instanceService.CreateInstanceAsync(request);
            
            return CreatedAtAction(nameof(GetInstanceForUser), new { userId = request.OwnerUserId }, new { instanceId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Unexpected error: {e.Message}");
        }
    }
    
    [HttpGet("my-instance")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetMyInstance()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var instance = await _instanceService.GetInstanceByOwnerIdAsync(int.Parse(userId));
        if (instance == null)
        {
            return NotFound(new { message = "No tienes ninguna instancia asignada." });
        }

        return Ok(instance);
    }
    
    [HttpPost("query")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> ExecuteQuery([FromBody] UserQueryRequestDto queryRequest)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var queryResult = await _instanceService.ExecuteUserQueryAsync(int.Parse(userId), queryRequest.Query);
            if (!queryResult.Success)
            {
                return BadRequest(queryResult);
            }
            return Ok(queryResult);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception e)
        {
            return StatusCode(500, $"Unexpected error: {e.Message}");
        }
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllInstances()
    {
        var instances = await _instanceService.GetAllInstancesAsync();
        return Ok(instances);
    }
    
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInstanceForUser(int userId)
    {
        var instance = await _instanceService.GetInstanceByOwnerIdAsync(userId);
        if (instance == null)
        {
            return NotFound(new { message = "El usuario no tiene una instancia asignada." });
        }
        return Ok(instance);
    }
}
