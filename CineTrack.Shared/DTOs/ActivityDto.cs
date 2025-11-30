namespace CineTrack.Shared.DTOs;

public class ActivityDto
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string? Username { get; set; }
	public string? UserAvatar { get; set; }
	public string? ActionType { get; set; }
	public string? TargetId { get; set; }
	public string? TargetTitle { get; set; }
	public string? ContentType { get; set; }
	public string? CoverUrl { get; set; }
	public DateTime CreatedAt { get; set; }
	public int? RatingValue { get; set; }
	public string? ReviewText { get; set; }
	public int? ReviewId { get; set; }
	public int LikeCount { get; set; }
	public int CommentCount { get; set; }
	public bool IsLiked { get; set; }
}