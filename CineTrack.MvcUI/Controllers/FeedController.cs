using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.MvcUI.Controllers;

public class FeedController : Controller
{
	private readonly ApiService _api;

	public FeedController(ApiService api)
	{
		_api = api;
	}

	[HttpPost]
	public async Task<IActionResult> ToggleLike(int id)
	{
		// Web API'ye yönlendir
		var result = await _api.PostAsync<object>($"api/activity/{id}/like", null);

		// Eğer API null dönerse (hata varsa), boş JSON obje dön ki JS patlamasın ama hatayı anlasın
		if (result == null) return BadRequest(new { success = false });

		return Content(result, "application/json");
	}

	[HttpGet]
	public async Task<IActionResult> GetComments(int id)
	{
		var comments = await _api.GetAsync<List<dynamic>>($"api/activity/{id}/comments");
		return Json(comments);
	}

	[HttpPost]
	public async Task<IActionResult> AddComment(int id, string text)
	{
		var payload = new { text };
		var result = await _api.PostAsync($"api/activity/{id}/comment", payload);
		return Content(result ?? "{}", "application/json");
	}
}