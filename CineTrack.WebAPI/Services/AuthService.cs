using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using CineTrack.WebAPI.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CineTrack.WebAPI.Services;

public class AuthService
{
	private readonly CineTrackDbContext _context;
	private readonly JwtService _jwtService;

	public AuthService(CineTrackDbContext context, JwtService jwtService)
	{
		_context = context;
		_jwtService = jwtService;
	}

	public async Task<AuthResponseDto> RegisterAsync(UserRegisterDto dto)
	{
		if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
			throw new Exception("Bu e-posta zaten kullaniliyor.");

		var user = new User
		{
			Username = dto.Username,
			Email = dto.Email,
			PasswordHash = HashPassword(dto.Password),
			AvatarUrl = dto.AvatarUrl
		};

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		var token = _jwtService.GenerateToken(user);

		return new AuthResponseDto
		{
			Token = token,
			User = new UserResponseDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				AvatarUrl = user.AvatarUrl,
				Bio = user.Bio,
				CreatedAt = user.CreatedAt
			}
		};
	}

	public async Task<AuthResponseDto> LoginAsync(UserLoginDto dto)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
		if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
			throw new Exception("E-posta veya sifre hatali.");

		var token = _jwtService.GenerateToken(user);

		return new AuthResponseDto
		{
			Token = token,
			User = new UserResponseDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				AvatarUrl = user.AvatarUrl,
				Bio = user.Bio,
				CreatedAt = user.CreatedAt
			}
		};
	}

	private string HashPassword(string password)
	{
		using var sha = SHA256.Create();
		var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
		return Convert.ToBase64String(bytes);
	}

	private bool VerifyPassword(string password, string hash)
	{
		return HashPassword(password) == hash;
	}
}
