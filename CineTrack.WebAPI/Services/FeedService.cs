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
		// 1. Takip edilenler mantığını kaldırıyoruz, global akışa dönüyoruz.
		// Sadece Rating ve Review aktivitelerini filtreliyoruz.
		var activities = await _context.ActivityLogs
			.Include(a => a.User)
			.Where(a => a.ActionType == "rating" || a.ActionType == "review") // Sadece yorum ve puanlar
			.OrderByDescending(a => a.CreatedAt)
			.Take(50) // Performans için limit (örneğin son 50 işlem)
			.ToListAsync();

		// 2. İçerik bilgilerini çek (Toplu sorgu - Performance optimization)
		var contentIds = activities
			.Select(a => a.ContentId)
			.Distinct()
			.Where(id => id != null)
			.ToList();

		var contents = await _context.Contents
			.Where(c => contentIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		// 3. Detay verilerini (Rating/Review) çekmek için listeyi hazırla
		var resultList = new List<ActivityDto>();

		foreach (var a in activities)
		{
			// İçerik veritabanında yoksa (silinmiş vs.) bu aktiviteyi atla
			if (a.ContentId == null || !contents.ContainsKey(a.ContentId)) continue;

			var content = contents[a.ContentId];

			var dto = new ActivityDto
			{
				UserId = a.UserId,
				Username = a.User!.Username,
				UserAvatar = a.User.AvatarUrl,
				ActionType = a.ActionType,
				TargetId = a.ContentId,
				TargetTitle = content.Title,
				ContentType = content.ContentType,
				CoverUrl = content.CoverUrl,
				CreatedAt = a.CreatedAt
			};

			// Eğer Rating ise Puanı bul
			if (a.ActionType == "rating")
			{
				var rating = await _context.Ratings
					.FirstOrDefaultAsync(r => r.UserId == a.UserId && r.ContentId == a.ContentId);
				if (rating != null) dto.RatingValue = rating.RatingValue;
			}

			// Eğer Review ise Yorumu bul
			if (a.ActionType == "review")
			{
				var review = await _context.Reviews
					.OrderByDescending(r => r.CreatedAt) // Varsa en son yorumu al
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
