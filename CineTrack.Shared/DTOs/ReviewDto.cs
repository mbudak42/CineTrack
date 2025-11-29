namespace CineTrack.Shared.DTOs;
public class ReviewDto
{
	public int? Id { get; set; }
	public string ContentId { get; set; }
	public string Text { get; set; }
	public DateTime CreatedAt { get; set; }
	public int UserId { get; set; }
	public string Username { get; set; }
}
