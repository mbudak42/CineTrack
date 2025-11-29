using Microsoft.AspNetCore.Mvc;
using CineTrack.Shared.DTOs;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly AuthService _authService;

	public AuthController(AuthService authService)
	{
		_authService = authService;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register(UserRegisterDto dto)
	{
		try
		{
			var result = await _authService.RegisterAsync(dto);
			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login(UserLoginDto dto)
	{
		try
		{
			var result = await _authService.LoginAsync(dto);
			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}
}
