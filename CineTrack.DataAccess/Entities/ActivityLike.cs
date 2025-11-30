namespace CineTrack.DataAccess.Entities;

public class ActivityLike
{
	public int UserId { get; set; }
	public int ActivityId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public User User { get; set; }
	public ActivityLog Activity { get; set; }
}