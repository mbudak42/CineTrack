namespace CineTrack.Shared.DTOs;
public class ReviewResponseDto
{
	public int Id { get; set; }
	public string ContentId { get; set; }
	public string ReviewText { get; set; }
	public string Username { get; set; }
	public DateTime CreatedAt { get; set; }
}
