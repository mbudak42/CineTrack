using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.MvcUI.Controllers;

public class UserListController : Controller
{
	private readonly ApiService _api;

	public UserListController(ApiService api)
	{
		_api = api;
	}

	// 1. Yeni Liste Oluştur
	[HttpPost]
	public async Task<IActionResult> Create(string name, string description)
	{
		if (string.IsNullOrWhiteSpace(name))
			return Json(new { success = false, message = "Liste adı boş olamaz." });

		var payload = new { name, description };
		// WebAPI'deki CreateListDto yapısına uygun gönderiyoruz
		var response = await _api.PostAsync("api/userlist", payload);

		if (response != null)
			return Json(new { success = true, message = "Liste başarıyla oluşturuldu." });

		return Json(new { success = false, message = "Liste oluşturulurken bir hata oluştu." });
	}

	// 2. Kullanıcının Listelerini Getir (JSON olarak - Modal içi doldurmak için)
	[HttpGet]
	public async Task<IActionResult> GetMyLists()
	{
		var lists = await _api.GetAsync<List<dynamic>>("api/userlist");
		return Json(lists ?? new List<dynamic>());
	}

	// 3. Özel Listeye İçerik Ekle
	[HttpPost]
	public async Task<IActionResult> AddContent(int listId, string contentId)
	{
		// WebAPI'deki AddContentDto yapısına uygun gönderiyoruz
		var payload = new { contentId };
		var response = await _api.PostAsync($"api/userlist/{listId}/add", payload);

		if (response != null)
			return Json(new { success = true, message = "İçerik listeye eklendi." });

		return Json(new { success = false, message = "İçerik eklenemedi veya zaten listede." });
	}

	// 4. Liste Detaylarını Getir (İçerikleriyle birlikte)
	[HttpGet]
	public async Task<IActionResult> GetListDetails(int id)
	{
		// WebAPI'den liste detayını çekiyoruz
		var listDetail = await _api.GetAsync<CineTrack.Shared.DTOs.UserListDto>($"api/userlist/{id}");

		if (listDetail == null)
			return Json(new { success = false, message = "Liste bulunamadı." });

		return Json(new { success = true, data = listDetail });
	}

	[HttpPost]
	public async Task<IActionResult> UpdateProfile(string bio, string avatarUrl)
	{
		var data = new { bio, avatarUrl };

		// API'ye güncelleme isteği at
		var response = await _api.PutAsync($"api/user/update", data);

		if (response != null)
		{
			// API'den dönen güncel kullanıcı bilgisini al
			using var doc = System.Text.Json.JsonDocument.Parse(response);
			var root = doc.RootElement;

			// Yeni AvatarUrl'i Session'a kaydet (Header resmi için)
			if (root.TryGetProperty("avatarUrl", out var avatarProp))
			{
				var newAvatar = avatarProp.GetString();
				HttpContext.Session.SetString("avatarUrl", newAvatar ?? "");
			}
		}

		// Profil sayfasına geri dön
		return RedirectToAction("Profile");
	}
}
