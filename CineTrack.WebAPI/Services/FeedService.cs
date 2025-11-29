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
	// ... mevcut usingler ...

	// GetFeedAsync metodunu şu şekilde güncelleyin:
	public async Task<List<ActivityDto>> GetFeedAsync()
	{
		var userId = GetUserId();

		// 1. Takip edilenleri bul
		var followingIds = await _context.Follows
			.Where(f => f.FollowerId == userId)
			.Select(f => f.FollowedId)
			.ToListAsync();

		followingIds.Add(userId); // Kendi aktivitelerimizi de görelim

		// 2. Aktiviteleri çek (User include edilmiş halde)
		var activities = await _context.ActivityLogs
			.Include(a => a.User)
			.Where(a => followingIds.Contains(a.UserId))
			.OrderByDescending(a => a.CreatedAt)
			.Take(30)
			.ToListAsync();

		// 3. İçerik bilgilerini çek (Toplu sorgu - Performance optimization)
		var contentIds = activities.Select(a => a.ContentId).Distinct().Where(id => id != null).ToList();
		var contents = await _context.Contents
			.Where(c => contentIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		// 4. Detay verilerini (Rating/Review) çekmek için listeleri hazırla
		var resultList = new List<ActivityDto>();

		foreach (var a in activities)
		{
			var dto = new ActivityDto
			{
				UserId = a.UserId,
				Username = a.User!.Username,
				UserAvatar = a.User.AvatarUrl, // Avatar eklendi
				ActionType = a.ActionType,
				TargetId = a.ContentId,
				TargetTitle = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].Title : "Bilinmiyor",
				ContentType = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].ContentType : null,
				CoverUrl = contents.ContainsKey(a.ContentId) ? contents[a.ContentId].CoverUrl : null,
				CreatedAt = a.CreatedAt
			};

			// Eğer Rating ise Puanı bul
			if (a.ActionType == "rating" && a.ContentId != null)
			{
				var rating = await _context.Ratings
					.FirstOrDefaultAsync(r => r.UserId == a.UserId && r.ContentId == a.ContentId);
				if (rating != null) dto.RatingValue = rating.RatingValue;
			}

			// Eğer Review ise Yorumu bul
			if (a.ActionType == "review" && a.ContentId != null)
			{
				// En son yorumu alalım
				var review = await _context.Reviews
					.OrderByDescending(r => r.CreatedAt)
					.FirstOrDefaultAsync(r => r.UserId == a.UserId && r.ContentId == a.ContentId);

				if (review != null)
				{
					dto.ReviewText = review.ReviewText;
					dto.ReviewId = review.Id;
				}
			}

			resultList.Add(dto);
		}

		return resultList;
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
