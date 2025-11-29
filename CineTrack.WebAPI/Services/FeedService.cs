using CineTrack.DataAccess;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTrack.WebAPI.Services;

public class FeedService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpContextAccessor _http;

	public FeedService(CineTrackDbContext context, IHttpContextAccessor http)
	{
		_context = context;
		_http = http;
	}

	private int GetUserId()
	{
		var id = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return id != null ? int.Parse(id) : 0;
	}

	// 1) Takip edilen kullanıcıların aktivitelerini getir
	public async Task<List<ActivityDto>> GetFeedAsync()
	{
		var userId = GetUserId();

		// Kullanıcının takip ettiği kişileri bul
		var followingIds = await _context.Follows
			.Where(f => f.FollowerId == userId)
			.Select(f => f.FollowedId)
			.ToListAsync();

		if (followingIds.Count == 0)
			return new List<ActivityDto>();

		// Bu kişilerin aktivitelerini çek
		var activities = await _context.ActivityLogs
			.Include(a => a.User)
			.Where(a => followingIds.Contains(a.UserId))
			.OrderByDescending(a => a.CreatedAt)
			.Take(30)
			.ToListAsync();

		// Content bilgilerini join et
		var contentIds = activities.Select(a => a.ContentId).Distinct().ToList();
		var contents = await _context.Contents
			.Where(c => contentIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		return activities.Select(a => new ActivityDto
		{
			UserId = a.UserId,
			Username = a.User!.Username,
			ActionType = a.ActionType,
			TargetId = a.ContentId,
			TargetTitle = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].Title : "Bilinmiyor",
			ContentType = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].ContentType : null,
			CoverUrl = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].CoverUrl : null,
			CreatedAt = a.CreatedAt
		}).ToList();
	}

	// 2) Belirli bir kullanıcının aktiviteleri
	public async Task<List<ActivityDto>> GetUserActivitiesAsync(int userId)
	{
		var activities = await _context.ActivityLogs
			.Include(a => a.User)
			.Where(a => a.UserId == userId)
			.OrderByDescending(a => a.CreatedAt)
			.Take(30)
			.ToListAsync();

		var contentIds = activities.Select(a => a.ContentId).Distinct().ToList();
		var contents = await _context.Contents
			.Where(c => contentIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		return activities.Select(a => new ActivityDto
		{
			UserId = a.UserId,
			Username = a.User!.Username,
			ActionType = a.ActionType,
			TargetId = a.ContentId,
			TargetTitle = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].Title : "Bilinmiyor",
			ContentType = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].ContentType : null,
			CoverUrl = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].CoverUrl : null,
			CreatedAt = a.CreatedAt
		}).ToList();
	}
}
