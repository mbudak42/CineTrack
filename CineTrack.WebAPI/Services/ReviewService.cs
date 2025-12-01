using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTrack.WebAPI.Services;

public class ReviewService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpContextAccessor _http;

	public ReviewService(CineTrackDbContext context, IHttpContextAccessor http)
	{
		_context = context;
		_http = http;
	}

	private int GetUserId()
	{
		var id = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return id != null ? int.Parse(id) : 0;
	}

	// 1) Yorum ekle veya güncelle
	public async Task<ReviewDto?> AddOrUpdateReviewAsync(ReviewDto dto)
	{
		var userId = GetUserId();
		var existing = await _context.Reviews
			.FirstOrDefaultAsync(r => r.UserId == userId && r.ContentId == dto.ContentId);

		if (existing != null)
		{
			existing.ReviewText = dto.Text;
			existing.CreatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
		}
		else
		{
			var review = new Review
			{
				UserId = userId,
				ContentId = dto.ContentId,
				ReviewText = dto.Text
			};
			_context.Reviews.Add(review);
			await _context.SaveChangesAsync();
		}

		var user = await _context.Users.FindAsync(userId);
		_context.ActivityLogs.Add(new ActivityLog
		{
			UserId = userId,
			ActionType = "review",
			ContentId = dto.ContentId
		});
		await _context.SaveChangesAsync();
		return new ReviewDto
		{
			ContentId = dto.ContentId,
			Text = dto.Text,
			UserId = userId,
			Username = user!.Username,
			CreatedAt = DateTime.UtcNow.AddHours(3)
		};

	}

	// 2) Belirli içeriğin tüm yorumlarını getir
	public async Task<List<ReviewDto>> GetReviewsByContentAsync(string contentId)
	{
		var reviews = await _context.Reviews
			.Include(r => r.User)
			.Where(r => r.ContentId == contentId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();

		return reviews.Select(r => new ReviewDto
		{
			Id = r.Id,
			ContentId = r.ContentId,
			Text = r.ReviewText,
			UserId = r.UserId,
			Username = r.User!.Username,
			CreatedAt = r.CreatedAt.AddHours(3)
		}).ToList();
	}

	// 3) Belirli bir kullanıcının yorumlarını getir
	public async Task<List<ReviewDto>> GetReviewsByUserAsync(int userId)
	{
		var reviews = await _context.Reviews
			.Include(r => r.Content)
			.Where(r => r.UserId == userId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();

		return reviews.Select(r => new ReviewDto
		{
			Id = r.Id,
			ContentId = r.ContentId,
			Text = r.ReviewText,
			UserId = userId,
			Username = r.User!.Username,
			CreatedAt = r.CreatedAt.AddHours(3)
		}).ToList();
	}

	public async Task<bool> DeleteReviewAsync(string contentId)
	{
		var userId = GetUserId();
		var review = await _context.Reviews
			.FirstOrDefaultAsync(r => r.UserId == userId && r.ContentId == contentId);

		if (review == null) return false;

		_context.Reviews.Remove(review);

		// Varsa ilgili aktivite logunu da temizleyelim
		var activity = await _context.ActivityLogs
			.FirstOrDefaultAsync(a => a.UserId == userId && a.ContentId == contentId && a.ActionType == "review");

		if (activity != null) _context.ActivityLogs.Remove(activity);

		await _context.SaveChangesAsync();
		return true;
	}
}
