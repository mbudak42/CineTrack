using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTrack.WebAPI.Services;

public class RatingService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpContextAccessor _http;

	public RatingService(CineTrackDbContext context, IHttpContextAccessor http)
	{
		_context = context;
		_http = http;
	}

	private int GetUserId()
	{
		var id = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return id != null ? int.Parse(id) : 0;
	}

	// 1) İçeriğe puan ver
	public async Task<bool> SetRatingAsync(RatingDto dto)
	{
		var userId = GetUserId();

		var existing = await _context.Ratings
			.FirstOrDefaultAsync(r => r.UserId == userId && r.ContentId == dto.ContentId);

		if (existing != null)
		{
			existing.RatingValue = dto.RatingValue;
			await _context.SaveChangesAsync();
			return true;
		}

		var rating = new Rating
		{
			UserId = userId,
			ContentId = dto.ContentId,
			RatingValue = dto.RatingValue
		};
		_context.Ratings.Add(rating);
		await _context.SaveChangesAsync();
		_context.ActivityLogs.Add(new ActivityLog
		{
			UserId = userId,
			ActionType = "rating",
			ContentId = dto.ContentId
		});
		await _context.SaveChangesAsync();

		return true;
	}

	// 2) İçeriğin ortalama puanını getir
	public async Task<double> GetAverageAsync(string contentId)
	{
		var ratings = await _context.Ratings
			.Where(r => r.ContentId == contentId)
			.ToListAsync();

		if (ratings.Count == 0) return 0;
		return Math.Round(ratings.Average(r => r.RatingValue), 2);
	}

	public async Task<int> GetUserRatingAsync(int userId, string contentId)
	{
		var rating = await _context.Ratings
			.FirstOrDefaultAsync(r => r.UserId == userId && r.ContentId == contentId);

		return rating?.RatingValue ?? 0;
	}
}
