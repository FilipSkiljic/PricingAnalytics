using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PricingAnalytics.Application.Services;

namespace PricingAnalytics.Api.Controllers;

// Validation attributes sit on the constructor parameters, not on the generated
// properties - MVC's record validation reads them from the primary constructor.
public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MinLength(2)] string BusinessName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

[ApiController]
[Route("auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>POST /auth/register - creates a business account and returns an access token.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.RegisterAsync(
                request.Email, request.Password, request.BusinessName, cancellationToken);
            return Ok(result);
        }
        catch (AuthenticationFailedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>POST /auth/login - exchanges credentials for an access token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
            return Ok(result);
        }
        catch (AuthenticationFailedException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
