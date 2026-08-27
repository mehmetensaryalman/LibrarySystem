using System.Security.Claims;
using LibrarySystem.Application.DTOs.Auth;
using LibrarySystem.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly
        IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService =
            authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult>
        Register(
            RegisterRequestDto request)
    {
        var result =
            await _authService
                .RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult>
        Login(
            LoginRequestDto request)
    {
        var result =
            await _authService
                .LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult>
        ForgotPassword(
            ForgotPasswordRequestDto request)
    {
        var result =
            await _authService
                .ForgotPasswordAsync(request);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult>
        ResetPassword(
            ResetPasswordRequestDto request)
    {
        var result =
            await _authService
                .ResetPasswordAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult>
        ChangePassword(
            ChangePasswordRequestDto request)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return Unauthorized();
        }

        var result =
            await _authService
                .ChangePasswordAsync(
                    userId,
                    request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}