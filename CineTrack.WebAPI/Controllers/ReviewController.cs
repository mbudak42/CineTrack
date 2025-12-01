using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.Shared.DTOs;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
	private readonly ReviewService _reviewService;

	public ReviewController(ReviewService reviewService)
	{
		_reviewService = reviewService;
	}

	[Authorize]
	[HttpPost]
	public async Task<IActionResult> AddOrUpdateReview([FromBody] ReviewDto dto)
	{
		var result = await _reviewService.AddOrUpdateReviewAsync(dto);
		return Ok(result);
	}

	[HttpGet("content/{contentId}")]
	public async Task<IActionResult> GetByContent(string contentId)
	{
		var reviews = await _reviewService.GetReviewsByContentAsync(contentId);
		return Ok(reviews);
	}

	[HttpGet("user/{userId}")]
	public async Task<IActionResult> GetByUser(int userId)
	{
		var reviews = await _reviewService.GetReviewsByUserAsync(userId);
		return Ok(reviews);
	}

	[Authorize]
	[HttpDelete("content/{contentId}")]
	public async Task<IActionResult> DeleteReview(string contentId)
	{
		var result = await _reviewService.DeleteReviewAsync(contentId);
		if (!result) return NotFound(new { message = "Yorum bulunamadı veya silinemedi." });
		return Ok(new { message = "Yorum silindi." });
	}
}
