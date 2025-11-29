using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;
using CineTrack.Shared.DTOs;

namespace CineTrack.MvcUI.Controllers;

public class HomeController : Controller
{
	private readonly ApiService _api;

	public HomeController(ApiService api)
	{
		_api = api;
	}

	public async Task<IActionResult> Index()
	{
		// Eğer kullanıcı giriş yapmamışsa basit bir karşılama sayfası göster
		var token = HttpContext.Session.GetString("token");

		if (string.IsNullOrEmpty(token))
		{
			return View(new List<ActivityDto>());
		}

		// Feed verilerini çek
		var feed = await _api.GetAsync<List<ActivityDto>>("api/feed");
		return View(feed ?? new List<ActivityDto>());
	}
}