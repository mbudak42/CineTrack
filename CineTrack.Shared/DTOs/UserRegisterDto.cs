namespace CineTrack.Shared.DTOs;
public class UserRegisterDto
{
	public string Username { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public string? AvatarUrl { get; set; }
}
