using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : ControllerBase
{
	private readonly FeedService _feedService;

	public ActivityController(FeedService feedService)
	{
		_feedService = feedService;
	}

	[HttpPost("{id}/like")]
	public async Task<IActionResult> ToggleLike(int id)
	{
		var isLiked = await _feedService.ToggleLikeAsync(id);
		return Ok(new { isLiked });
	}

	[HttpGet("{id}/comments")]
	public async Task<IActionResult> GetComments(int id)
	{
		var comments = await _feedService.GetCommentsAsync(id);
		return Ok(comments);
	}

	[HttpPost("{id}/comment")]
	public async Task<IActionResult> AddComment(int id, [FromBody] CommentRequest req)
	{
		if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest();
		var comment = await _feedService.AddCommentAsync(id, req.Text);
		return Ok(comment);
	}

	public class CommentRequest { public string Text { get; set; } }
}