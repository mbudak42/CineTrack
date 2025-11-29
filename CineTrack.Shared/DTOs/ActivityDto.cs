namespace CineTrack.Shared.DTOs;

public class ActivityDto
{
	public int UserId { get; set; }
	public string? Username { get; set; }
	public string? UserAvatar { get; set; }
	public string? ActionType { get; set; } // review | rating | list_add
	public string? TargetId { get; set; }   // content id
	public string? TargetTitle { get; set; }
	public string? ContentType { get; set; }
	public string? CoverUrl { get; set; }
	public DateTime CreatedAt { get; set; }
	public int? RatingValue { get; set; }
	public string? ReviewText { get; set; }
	public int? ReviewId { get; set; }
}
