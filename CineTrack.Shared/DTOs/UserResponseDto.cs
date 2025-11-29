namespace CineTrack.Shared.DTOs;
public class UserResponseDto
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string Email { get; set; }
	public string? AvatarUrl { get; set; }
	public string? Bio { get; set; }
	public DateTime CreatedAt { get; set; }
}
