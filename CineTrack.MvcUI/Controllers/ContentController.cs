using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.MvcUI.Controllers;

public class ContentController : Controller
{
	private readonly ApiService _api;

	public ContentController(ApiService api)
	{
		_api = api;
	}

	[HttpGet]
	public async Task<IActionResult> Search(string q, string type = "movie")
	{
		if (string.IsNullOrWhiteSpace(q)) return View(new List<dynamic>());
		string endpoint;
		if (type == "book")
			endpoint = $"api/content/search/books?q={q}";
		else
			endpoint = $"api/content/search/movies?q={q}";
		var results = await _api.GetAsync<List<dynamic>>(endpoint);
		return View(results);
	}

	[HttpGet]
	public async Task<IActionResult> Detail(string type, string id)
	{
		var content = await _api.GetAsync<dynamic>($"/api/content/{type}/{id}");
		return View(content);
	}
}
