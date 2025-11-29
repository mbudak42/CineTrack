using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.Shared.DTOs;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
	private readonly RatingService _ratingService;

	public RatingController(RatingService ratingService)
	{
		_ratingService = ratingService;
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
}
