using CineTrack.MvcUI.Models;
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

	// GET: /Content/Search?q=xxx&type=movie
	[HttpGet]
	public async Task<IActionResult> Search(string q, string type = "movie")
	{
		if (string.IsNullOrWhiteSpace(q))
			return View(new List<ContentCardDto>());

		string endpoint = type switch
		{
			"book" => $"api/content/search/books?q={q}",
			_ => $"api/content/search/movies?q={q}"
		};

		var results = await _api.GetAsync<List<ContentCardDto>>(endpoint);
		return View(results ?? new List<ContentCardDto>());
	}

	// GET: /Content/Detail?type=movie&id=123
	[HttpGet]
	public async Task<IActionResult> Detail(string type, string id)
	{
		if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(id))
			return NotFound();

		// Tip güvenli ViewModel ile al
		var content = await _api.GetAsync<ContentViewModel>($"/api/content/{type}/{id}");
		if (content == null)
			return NotFound();

		if (HttpContext.Session.GetString("token") != null)
		{
			// API'ye yeni eklediğimiz endpoint'e istek atıyoruz
			// Dönen JSON: { "rating": 5 }
			// Dynamic kullanarak alalım
			try
			{
				var ratingJson = await _api.GetAsync<dynamic>($"api/rating/my-rating/{id}");
				if (ratingJson != null)
				{
					// System.Text.Json.JsonElement olarak gelebilir, parse edelim
					var el = (System.Text.Json.JsonElement)ratingJson;
					if (el.TryGetProperty("rating", out var rVal))
					{
						content.CurrentUserRating = rVal.GetInt32();
					}
				}
			}
			catch { /* Hata olursa 0 kalsın */ }
		}

		// MetadataJson varsa parse edebilirsin ve ContentDetailDto alanlarını doldurabilirsin
		// Örneğin film süresi, yazar, yönetmen, özet gibi
		if (!string.IsNullOrEmpty(content.MetadataJson))
		{
			try
			{
				var doc = System.Text.Json.JsonDocument.Parse(content.MetadataJson);
				var root = doc.RootElement;

				if (content.ContentType == "movie")
				{
					content.Overview = root.GetProperty("overview").GetString();
					content.Year = root.GetProperty("release_date").GetString()?.Split('-')[0] is string y ? int.Parse(y) : null;
					content.DurationOrPageCount = root.TryGetProperty("runtime", out var runtime) ? runtime.GetInt32() : null;
					if (root.TryGetProperty("genres", out var genres))
						content.Genres = genres.EnumerateArray().Select(g => g.GetProperty("name").GetString() ?? "").ToList();
					if (root.TryGetProperty("credits", out var credits) && credits.TryGetProperty("crew", out var crew))
						content.Directors = crew.EnumerateArray()
												.Where(c => c.GetProperty("job").GetString() == "Director")
												.Select(c => c.GetProperty("name").GetString() ?? "")
												.ToList();
				}
				else if (content.ContentType == "book")
				{
					var info = root.GetProperty("volumeInfo");
					content.Overview = info.GetProperty("description").GetString();
					content.Authors = info.TryGetProperty("authors", out var authors)
									  ? authors.EnumerateArray().Select(a => a.GetString() ?? "").ToList()
									  : new List<string>();
					content.Year = info.TryGetProperty("publishedDate", out var date) && date.GetString()?.Length >= 4
								   ? int.Parse(date.GetString()!.Substring(0, 4))
								   : null;
					content.DurationOrPageCount = info.TryGetProperty("pageCount", out var pageCount) ? pageCount.GetInt32() : null;
					if (info.TryGetProperty("categories", out var categories))
						content.Genres = categories.EnumerateArray().Select(c => c.GetString() ?? "").ToList();
				}
			}
			catch
			{
				// Hata olursa boş bırak
			}
		}

		return View(content);
	}

	[HttpPost]
	public async Task<IActionResult> RateContent(string contentId, int rating)
	{
		var payload = new { contentId = contentId, ratingValue = rating };
		var response = await _api.PostAsync("api/rating", payload); // RatingController SetRating

		if (response != null)
			return Json(new { success = true, message = "Puan kaydedildi." });

		return Json(new { success = false, message = "Puan kaydedilirken hata oluştu." });
	}

	[HttpPost]
	public async Task<IActionResult> AddToList(string contentId, string listName)
	{
		// 1. Kullanıcı giriş yapmış mı kontrol et
		if (HttpContext.Session.GetString("token") == null)
		{
			return Json(new { success = false, message = "Lütfen önce giriş yapın." });
		}

		try
		{
			// 2. Kullanıcının mevcut listelerini çek
			var userLists = await _api.GetAsync<List<CineTrack.Shared.DTOs.UserListDto>>("api/userlist");

			// 3. İlgili isimdeki listeyi bul (Örn: "İzlediklerim")
			var targetList = userLists?.FirstOrDefault(l => l.Name == listName);
			int listId;

			// 4. Liste yoksa oluştur
			if (targetList == null)
			{
				var createPayload = new { name = listName, description = "Otomatik oluşturulan liste" };
				var createResponse = await _api.PostAsync("api/userlist", createPayload);

				if (string.IsNullOrEmpty(createResponse))
					return Json(new { success = false, message = "Liste oluşturulamadı." });

				// Oluşturulan listenin ID'sini al
				using var doc = System.Text.Json.JsonDocument.Parse(createResponse);
				listId = doc.RootElement.GetProperty("id").GetInt32();
			}
			else
			{
				listId = targetList.Id;
			}

			// 5. İçeriği listeye ekle
			var addPayload = new { contentId = contentId };
			// API endpoint: api/userlist/{id}/add
			var addResponse = await _api.PostAsync($"api/userlist/{listId}/add", addPayload);

			if (addResponse != null)
			{
				return Json(new { success = true, message = $"{listName} listenize eklendi!" });
			}
			else
			{
				// API BadRequest dönerse genelde null dönebilir (ApiService yapısına göre)
				// veya içerik zaten listede olabilir.
				return Json(new { success = false, message = "İşlem başarısız veya içerik zaten listede." });
			}
		}
		catch (Exception ex)
		{
			return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
		}
	}
}
