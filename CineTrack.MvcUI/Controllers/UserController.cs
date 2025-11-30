using CineTrack.MvcUI.Models;
using CineTrack.MvcUI.Services;
using CineTrack.Shared.DTOs; // API DTO'larını kullandığımızdan emin olun
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.MvcUI.Controllers;

public class UserController : Controller
{
	private readonly ApiService _api;

	public UserController(ApiService api)
	{
		_api = api;
	}

	[HttpGet]
	public async Task<IActionResult> Profile(int? id)
	{
		// 1. Görüntülenecek User ID'yi belirle
		var currentUserId = HttpContext.Session.GetInt32("userId");
		int targetUserId = id ?? currentUserId ?? 0;

		if (targetUserId == 0) return RedirectToAction("Login", "Auth");

		var model = new UserProfileViewModel();

		// 2. Kullanıcı Bilgilerini Çek
		model.User = await _api.GetAsync<UserResponseDto>($"api/user/{targetUserId}");
		if (model.User == null) return NotFound();

		// 3. Sahiplik ve Takip Durumu
		model.IsOwner = (currentUserId == targetUserId);

		if (!model.IsOwner && currentUserId.HasValue)
		{
			var myFollowing = await _api.GetAsync<List<UserResponseDto>>($"api/user/{currentUserId}/following");
			model.IsFollowing = myFollowing?.Any(u => u.Id == targetUserId) ?? false;
		}

		// 4. İstatistikler
		var followers = await _api.GetAsync<List<UserResponseDto>>($"api/user/{targetUserId}/followers");
		var following = await _api.GetAsync<List<UserResponseDto>>($"api/user/{targetUserId}/following");
		model.FollowersCount = followers?.Count ?? 0;
		model.FollowingCount = following?.Count ?? 0;

		// 5. Listeleri Çek ve Grupla (GÜNCELLENDİ)
		// Artık sadece IsOwner kontrolü YAPMIYORUZ. Herkes listeleri görebilir.
		// Yeni eklediğimiz API endpointini kullanıyoruz: api/userlist/user/{id}
		var allLists = await _api.GetAsync<List<UserListDto>>($"api/userlist/user/{targetUserId}");

		if (allLists != null)
		{
			foreach (var list in allLists)
			{
				switch (list.Name)
				{
					case "izlediklerim":
						model.WatchedMovies.AddRange(list.Contents ?? new());
						model.WatchedListId = list.Id;
						break;
					case "izlenecekler":
						model.WatchlistMovies.AddRange(list.Contents ?? new());
						model.WatchlistListId = list.Id;
						break;
					case "okuduklarim":
						model.ReadBooks.AddRange(list.Contents ?? new());
						model.ReadListId = list.Id;
						break;
					case "okunacaklar":
						model.ReadingListBooks.AddRange(list.Contents ?? new());
						model.ReadingListId = list.Id;
						break;
					default:
						model.CustomLists.Add(list);
						break;
				}
			}
		}

		// 6. Son Aktiviteler
		model.RecentActivities = await _api.GetAsync<List<ActivityDto>>($"api/feed/user/{targetUserId}") ?? new List<ActivityDto>();

		return View(model);
	}

	[HttpPost]
	public async Task<IActionResult> Follow(int id)
	{
		await _api.PostAsync<object>($"api/user/follow/{id}", null);
		return RedirectToAction("Profile", new { id = id });
	}

	[HttpPost]
	public async Task<IActionResult> Unfollow(int id)
	{
		await _api.PostAsync<object>($"api/user/unfollow/{id}", null);
		return RedirectToAction("Profile", new { id = id });
	}

	[HttpPost]
	public async Task<IActionResult> UpdateProfile(string bio, string avatarUrl)
	{
		var data = new { bio, avatarUrl };
		await _api.PutAsync($"api/user/update", data); // ApiService'e PutAsync eklenmesi gerekebilir veya Post kullanılır.
													   // Mevcut ApiService sadece Get ve Post içeriyor. Post kullanalım:
													   // Not: API "HttpPut" bekliyor. ApiService'e PutAsync metodu eklenmeli 
													   // veya HttpClient üzerinden manuel istek atılmalı. 
													   // Geçici çözüm olarak UpdateProfile action'ı API'de POST'a çevrilebilir veya ApiService güncellenir.
		return RedirectToAction("Profile");
	}
}