namespace CineTrack.Shared.DTOs;

public class ActivityCommentDto
{
	public int Id { get; set; }
	public int ActivityId { get; set; }
	public string Text { get; set; }
	public string Username { get; set; }
	public string? UserAvatar { get; set; }
	public DateTime CreatedAt { get; set; }
}