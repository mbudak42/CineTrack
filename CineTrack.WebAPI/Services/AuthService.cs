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
	private readonly EmailService _emailService;

	public AuthService(CineTrackDbContext context, JwtService jwtService, EmailService emailService)
	{
		_context = context;
		_jwtService = jwtService;
		_emailService = emailService;
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

	public async Task GeneratePasswordResetTokenAsync(string email)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
		if (user == null) return; // Güvenlik gereği kullanıcı yoksa bile hata dönmeyiz veya "bulunamadı" deriz.

		// Rastgele Token oluştur
		var token = Guid.NewGuid().ToString();
		user.PasswordResetToken = token;
		user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // 1 saat geçerli

		await _context.SaveChangesAsync();

		// Link: MVC UI adresine yönlendirmeli (localhost:5215 varsayılan port)
		var resetLink = $"http://localhost:5215/Auth/ResetPassword?token={token}&email={email}";

		var body = $"<h3>Şifre Sıfırlama</h3><p>Şifrenizi sıfırlamak için <a href='{resetLink}'>tıklayınız</a>.</p>";

		await _emailService.SendEmailAsync(email, "CineTrack Şifre Sıfırlama", body);
	}

	// 2. Şifreyi Sıfırla
	public async Task ResetPasswordAsync(ResetPasswordDto dto)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

		if (user == null || user.PasswordResetToken != dto.Token || user.PasswordResetTokenExpires < DateTime.UtcNow)
		{
			throw new Exception("Geçersiz veya süresi dolmuş token.");
		}

		user.PasswordHash = HashPassword(dto.NewPassword);
		user.PasswordResetToken = null;
		user.PasswordResetTokenExpires = null;

		await _context.SaveChangesAsync();
	}
}
