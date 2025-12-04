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

	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
	{
		await _authService.GeneratePasswordResetTokenAsync(dto.Email);
		return Ok(new { message = "Eğer kayıtlı bir e-posta ise sıfırlama bağlantısı gönderildi." });
	}

	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
	{
		try
		{
			await _authService.ResetPasswordAsync(dto);
			return Ok(new { message = "Şifreniz başarıyla güncellendi." });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = ex.Message });
		}
	}
}
