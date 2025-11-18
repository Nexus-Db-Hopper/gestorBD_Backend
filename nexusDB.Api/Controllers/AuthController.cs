using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nexusDB.Application.Dtos;
using nexusDB.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace nexusDB.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
    {
        var (succeeded, errorMessage) = await _authService.RegisterUserAsync(registerDto);
        if (!succeeded) return BadRequest(errorMessage);
        return StatusCode(201, "User registered successfully.");
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
    {
        var (succeeded, tokens, errorMessage) = await _authService.LoginUserAsync(loginDto);
        if (!succeeded) return Unauthorized(errorMessage);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var (succeeded, tokens, errorMessage) = await _authService.RefreshTokenAsync(refreshToken);
        if (!succeeded) return Unauthorized(errorMessage);
        return Ok(tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        
        await _authService.LogoutAsync(userId);
        return NoContent();
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userId == null) return Unauthorized();

        return Ok(new { Id = userId, Email = userEmail, Role = userRole });
    }

    [HttpGet("admin-data")]
    [Authorize(Roles = "Admin")] // <-- SOLO PARA ADMINS
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminData()
    {
        return Ok("This is a top secret message for administrators only!");
    }
}
