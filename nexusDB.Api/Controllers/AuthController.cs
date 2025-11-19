using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nexusDB.Application.Common;
using nexusDB.Application.Dtos;
using nexusDB.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace nexusDB.Api.Controllers;

/// <summary>
/// Controlador "delgado" para la autenticación. Orquesta el flujo HTTP y delega la lógica de negocio.
/// Sigue las mejores prácticas de API REST devolviendo respuestas HTTP estándar y ProblemDetails para errores.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
    {
        var result = await _authService.RegisterUserAsync(registerDto);
        return HandleResult(result, () => StatusCode(201, "User registered successfully."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
    {
        var result = await _authService.LoginUserAsync(loginDto);
        return HandleResult(result, Ok, Unauthorized);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var result = await _authService.RefreshTokenAsync(refreshToken);
        return HandleResult(result, Ok, Unauthorized);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new ProblemDetails { Title = "Logout Failed", Detail = "User identifier not found in token." });
        }
        
        var result = await _authService.LogoutAsync(userId);
        return HandleResult(result, NoContent);
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetProfile()
    {
        var userProfile = new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role)
        };
        return Ok(userProfile);
    }

    [HttpGet("admin-data")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        return Ok("This is a top secret message for administrators only!");
    }

    // --- MÉTODOS AUXILIARES PARA MANEJO DE RESPUESTAS ---

    private IActionResult HandleResult<T>(Result<T> result, Func<T, IActionResult> onSuccess, Func<ProblemDetails, IActionResult>? onError = null)
    {
        if (result.IsFailure)
        {
            var problemDetails = new ProblemDetails { Title = "An error occurred", Detail = result.Error };
            return onError != null ? onError(problemDetails) : BadRequest(problemDetails);
        }
        return onSuccess(result.Value!);
    }

    private IActionResult HandleResult(Result result, Func<IActionResult> onSuccess, Func<ProblemDetails, IActionResult>? onError = null)
    {
        if (result.IsFailure)
        {
            var problemDetails = new ProblemDetails { Title = "An error occurred", Detail = result.Error };
            return onError != null ? onError(problemDetails) : BadRequest(problemDetails);
        }
        return onSuccess();
    }
}
