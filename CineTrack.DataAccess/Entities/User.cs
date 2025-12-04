namespace CineTrack.DataAccess.Entities;

public class User
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public string? AvatarUrl { get; set; }
	public string? Bio { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public string? PasswordResetToken { get; set; }
	public DateTime? PasswordResetTokenExpires { get; set; }
	public ICollection<Review>? Reviews { get; set; }
	public ICollection<Rating>? Ratings { get; set; }
	public ICollection<UserList>? UserLists { get; set; }
	public ICollection<ActivityLog>? Activities { get; set; }
	public ICollection<Follow>? Followers { get; set; }
	public ICollection<Follow>? Following { get; set; }
}
