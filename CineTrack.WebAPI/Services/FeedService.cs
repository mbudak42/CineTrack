using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
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

	// Helper: Token'dan User ID okuma
	private int GetUserId()
	{
		var id = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return id != null ? int.Parse(id) : 0;
	}

	// 1) Ana Akış (Feed) Getir - Beğeni ve Yorum verileriyle
	public async Task<List<ActivityDto>> GetFeedAsync(int page = 1, int pageSize = 15)
	{
		var currentUserId = GetUserId();

		// Rating ve Review aktivitelerini çek
		var activities = await _context.ActivityLogs
		.Include(a => a.User)
		.Where(a => a.ActionType == "rating" || a.ActionType == "review")
		.OrderByDescending(a => a.CreatedAt)
		.Skip((page - 1) * pageSize) // Atla
		.Take(pageSize)              // Al
		.ToListAsync();

		// İlgili içeriklerin detaylarını (Title, CoverUrl vb.) topluca çek
		var contentIds = activities
			.Select(a => a.ContentId)
			.Distinct()
			.Where(id => id != null)
			.ToList();

		var contents = await _context.Contents
			.Where(c => contentIds.Contains(c.Id))
			.ToDictionaryAsync(c => c.Id);

		var resultList = new List<ActivityDto>();

		foreach (var a in activities)
		{
			if (a.ContentId == null || !contents.ContainsKey(a.ContentId)) continue;

			var content = contents[a.ContentId];

			// --- Beğeni ve Yorum İstatistikleri ---
			// Not: Performans için bu sorgular ilerde GroupBy ile toplu yapılabilir.
			var likeCount = await _context.ActivityLikes.CountAsync(l => l.ActivityId == a.Id);
			var commentCount = await _context.ActivityComments.CountAsync(c => c.ActivityId == a.Id);
			var isLiked = await _context.ActivityLikes.AnyAsync(l => l.ActivityId == a.Id && l.UserId == currentUserId);

			var dto = new ActivityDto
			{
				Id = a.Id, // Aktivite ID'si (Önemli: Beğeni/Yorum için gerekli)
				UserId = a.UserId,
				Username = a.User!.Username,
				UserAvatar = a.User.AvatarUrl,
				ActionType = a.ActionType,
				TargetId = a.ContentId,
				TargetTitle = content.Title,
				ContentType = content.ContentType,
				CoverUrl = content.CoverUrl,
				CreatedAt = a.CreatedAt.AddHours(3),

				// Yeni Alanlar
				LikeCount = likeCount,
				CommentCount = commentCount,
				IsLiked = isLiked
			};

			// Rating ise puanı bul
			if (a.ActionType == "rating")
			{
				var rating = await _context.Ratings
					.FirstOrDefaultAsync(r => r.UserId == a.UserId && r.ContentId == a.ContentId);
				if (rating != null) dto.RatingValue = rating.RatingValue;
			}

			// Review ise yorumu bul
			if (a.ActionType == "review")
			{
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
		var currentUserId = GetUserId();

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

		var resultList = new List<ActivityDto>();

		foreach (var a in activities)
		{
			// İçerik silinmişse atla
			if (a.ContentId == null || !contents.ContainsKey(a.ContentId)) continue;

			var content = contents[a.ContentId];

			// Profil sayfasında da beğeni durumlarını görmek istersek:
			var likeCount = await _context.ActivityLikes.CountAsync(l => l.ActivityId == a.Id);
			var commentCount = await _context.ActivityComments.CountAsync(c => c.ActivityId == a.Id);
			var isLiked = await _context.ActivityLikes.AnyAsync(l => l.ActivityId == a.Id && l.UserId == currentUserId);

			resultList.Add(new ActivityDto
			{
				Id = a.Id,
				UserId = a.UserId,
				Username = a.User!.Username,
				UserAvatar = a.User.AvatarUrl,
				ActionType = a.ActionType,
				TargetId = a.ContentId,
				TargetTitle = content.Title,
				ContentType = content.ContentType,
				CoverUrl = content.CoverUrl,
				CreatedAt = a.CreatedAt.AddHours(3),
				LikeCount = likeCount,
				CommentCount = commentCount,
				IsLiked = isLiked
			});
		}

		return resultList;
	}

	// 3) Beğeni İşlemi (Toggle: Varsa siler, yoksa ekler)
	public async Task<bool> ToggleLikeAsync(int activityId)
	{
		var userId = GetUserId();
		if (userId == 0) return false;

		var existing = await _context.ActivityLikes
			.FirstOrDefaultAsync(l => l.ActivityId == activityId && l.UserId == userId);

		if (existing != null)
		{
			// Zaten beğenilmiş -> Beğeniyi kaldır
			_context.ActivityLikes.Remove(existing);
			await _context.SaveChangesAsync();
			return false; // Artık beğenilmiyor
		}

		// Beğenilmemiş -> Beğeni ekle
		_context.ActivityLikes.Add(new ActivityLike
		{
			ActivityId = activityId,
			UserId = userId
		});
		await _context.SaveChangesAsync();
		return true; // Beğenildi
	}

	// 4) Yorum Ekleme
	public async Task<ActivityCommentDto> AddCommentAsync(int activityId, string text)
	{
		var userId = GetUserId();
		var user = await _context.Users.FindAsync(userId);

		var comment = new ActivityComment
		{
			ActivityId = activityId,
			UserId = userId,
			Text = text,
			CreatedAt = DateTime.UtcNow
		};

		_context.ActivityComments.Add(comment);
		await _context.SaveChangesAsync();

		return new ActivityCommentDto
		{
			Id = comment.Id,
			ActivityId = activityId,
			Text = text,
			Username = user!.Username,
			UserAvatar = user.AvatarUrl,
			CreatedAt = comment.CreatedAt
		};
	}

	// 5) Yorumları Listeleme
	public async Task<List<ActivityCommentDto>> GetCommentsAsync(int activityId)
	{
		return await _context.ActivityComments
			.Include(c => c.User)
			.Where(c => c.ActivityId == activityId)
			.OrderBy(c => c.CreatedAt) // Eskiden yeniye doğru
			.Select(c => new ActivityCommentDto
			{
				Id = c.Id,
				ActivityId = c.ActivityId,
				Text = c.Text,
				Username = c.User.Username,
				UserAvatar = c.User.AvatarUrl,
				CreatedAt = c.CreatedAt
			}).ToListAsync();
	}
}
