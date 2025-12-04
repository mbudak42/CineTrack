using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.Shared.DTOs;
using CineTrack.WebAPI.Services;
using System.Security.Claims; // Eklendi

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
	private readonly RatingService _ratingService;
	private readonly IHttpContextAccessor _http; // Eklendi

	public RatingController(RatingService ratingService, IHttpContextAccessor http)
	{
		_ratingService = ratingService;
		_http = http;
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> SetRating([FromBody] RatingDto dto)
	{
		var result = await _ratingService.SetRatingAsync(dto);
		if (!result)
			return BadRequest(new { message = "Puan eklenemedi." });

		return Ok(new { message = "Puan kaydedildi." });
	}

	[HttpGet("{contentId}")]
	public async Task<IActionResult> GetAverage(string contentId)
	{
		double avg = await _ratingService.GetAverageAsync(contentId);
		return Ok(new { average = avg });
	}

	[Authorize]
	[HttpGet("my-rating/{contentId}")]
	public async Task<IActionResult> GetMyRating(string contentId)
	{
		// Token'dan userId al
		var userIdStr = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdStr)) return Ok(new { rating = 0 });

		int userId = int.Parse(userIdStr); var rating = await _ratingService.GetUserRatingAsync(userId, contentId);
		return Ok(new { rating = rating });
	}

	[HttpGet("stats/{contentId}")]
	public async Task<IActionResult> GetRatingStats(string contentId)
	{
		var stats = await _ratingService.GetRatingStatsAsync(contentId);
		return Ok(new { average = stats.Average, count = stats.Count });
	}
}
