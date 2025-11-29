namespace CineTrack.Shared.DTOs;
public class UserListDto
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string? Description { get; set; }
	public List<ContentDto>? Contents { get; set; }
}
