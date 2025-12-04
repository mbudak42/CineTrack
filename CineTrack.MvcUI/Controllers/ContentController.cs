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

		var content = await _api.GetAsync<ContentViewModel>($"/api/content/{type}/{id}");
		if (content == null) return NotFound();

		// --- 1. UYGULAMA İÇİ PUANLAMA BİLGİLERİ (GÜNCELLENDİ) ---
		try
		{
			// Yeni eklediğimiz 'stats' endpoint'ine istek atıyoruz
			var statsElement = await _api.GetAsync<System.Text.Json.JsonElement?>($"api/rating/stats/{id}");

			if (statsElement.HasValue)
			{
				var el = statsElement.Value;
				// API'den dönen { average: 8.5, count: 120 } yapısını okuyoruz
				if (el.TryGetProperty("average", out var avgVal)) content.AverageRating = avgVal.GetDouble();
				if (el.TryGetProperty("count", out var cntVal)) content.RatingCount = cntVal.GetInt32();
			}

			// Kullanıcı giriş yapmışsa kendi puanını da çek
			if (HttpContext.Session.GetString("token") != null)
			{
				var myRatingEl = await _api.GetAsync<System.Text.Json.JsonElement?>($"api/rating/my-rating/{id}");
				if (myRatingEl.HasValue && myRatingEl.Value.TryGetProperty("rating", out var rVal))
				{
					content.CurrentUserRating = rVal.GetInt32();
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Rating stats error: {ex.Message}");
		}

		// --- 2. METADATA JSON PARSE İŞLEMİ (GÜNCELLENDİ) ---
		if (!string.IsNullOrEmpty(content.MetadataJson))
		{
			try
			{
				var doc = System.Text.Json.JsonDocument.Parse(content.MetadataJson);
				var root = doc.RootElement;

				if (content.ContentType == "movie")
				{
					// ... Mevcut Overview, Year, Duration kodları burada kalsın ...
					content.Overview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : "";
					content.Year = root.GetProperty("release_date").GetString()?.Split('-')[0] is string y ? int.Parse(y) : null;
					content.DurationOrPageCount = root.TryGetProperty("runtime", out var runtime) ? runtime.GetInt32() : null;

					if (root.TryGetProperty("genres", out var genres))
						content.Genres = genres.EnumerateArray().Select(g => g.GetProperty("name").GetString() ?? "").ToList();

					// Yönetmen (Mevcut kod)
					if (root.TryGetProperty("credits", out var credits))
					{
						if (credits.TryGetProperty("crew", out var crew))
						{
							content.Directors = crew.EnumerateArray()
								.Where(c => c.GetProperty("job").GetString() == "Director")
								.Select(c => c.GetProperty("name").GetString() ?? "")
								.Take(2) // Çok fazla yönetmen varsa ilk 2'sini al
								.ToList();
						}

						// --- YENİ EKLENEN: OYUNCULAR (CAST) ---
						if (credits.TryGetProperty("cast", out var cast))
						{
							content.Cast = cast.EnumerateArray()
								.Select(c => c.GetProperty("name").GetString() ?? "")
								.Take(10) // İlk 10 oyuncuyu gösterelim
								.ToList();
						}
					}
				}
				// ... Kitap parse işlemleri (Book) aynı kalabilir ...
				else if (content.ContentType == "book")
				{
					// Kitap kodları buraya (değişiklik yok)
					var info = root.GetProperty("volumeInfo");
					content.Overview = info.TryGetProperty("description", out var d) ? d.GetString() : "";

					if (info.TryGetProperty("authors", out var authorsProp))
					{
						content.Authors = authorsProp.EnumerateArray()
							.Select(a => a.GetString() ?? "")
							.ToList();
					}

					// Sayfa sayısı (isteğe bağlı, zaten varsa kalsın)
					content.DurationOrPageCount = info.TryGetProperty("pageCount", out var pc) ? pc.GetInt32() : null;

					// Yayın tarihi/yılı (isteğe bağlı)
					if (info.TryGetProperty("publishedDate", out var pd))
					{
						var dateStr = pd.GetString();
						if (!string.IsNullOrEmpty(dateStr) && dateStr.Length >= 4)
						{
							content.Year = int.Parse(dateStr.Substring(0, 4));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Metadata parse error: {ex.Message}");
			}
		}

		// Yorumları çekme kısmı (aynen kalsın)
		// ...
		try
		{
			var reviews = await _api.GetAsync<List<CineTrack.Shared.DTOs.ReviewDto>>($"api/review/content/{id}");
			if (reviews != null)
			{
				content.Comments = reviews.Select(r => new CommentDto { UserName = r.Username, Text = r.Text, CreatedAt = r.CreatedAt }).ToList();
				var currentUsername = HttpContext.Session.GetString("username");
				if (!string.IsNullOrEmpty(currentUsername))
				{
					var myReview = reviews.FirstOrDefault(r => r.Username == currentUsername);
					if (myReview != null) content.CurrentUserReview = myReview.Text;
				}
			}
		}
		catch { }

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

	[HttpPost]
	public async Task<IActionResult> AddComment(string contentId, string text)
	{
		if (HttpContext.Session.GetString("token") == null)
		{
			return Json(new { success = false, message = "Lütfen önce giriş yapın." });
		}

		if (string.IsNullOrWhiteSpace(text))
		{
			return Json(new { success = false, message = "Yorum boş olamaz." });
		}

		var payload = new CineTrack.Shared.DTOs.ReviewCreateDto
		{
			ContentId = contentId,
			ReviewText = text
		};

		// ReviewController'daki AddOrUpdateReview endpoint'i 'api/review' adresinde
		// Ancak o metot ReviewDto bekliyor, CreateDto değil. 
		// Bu yüzden API tarafındaki ReviewController veya DTO uyumunu kontrol edelim.
		// Mevcut ReviewController [FromBody] ReviewDto bekliyor.
		// O yüzden payload'ı ona uyduruyoruz:

		var apiPayload = new CineTrack.Shared.DTOs.ReviewDto
		{
			ContentId = contentId,
			Text = text
			// UserId token'dan alınacak, Id otomatik artacak
		};

		var response = await _api.PostAsync("api/review", apiPayload);

		if (response != null)
			return Json(new { success = true, message = "Yorumunuz eklendi." });

		return Json(new { success = false, message = "Yorum eklenirken hata oluştu." });
	}

	// ContentController.cs içine eklenecek
	[HttpPost]
	public async Task<IActionResult> DeleteReview(string contentId)
	{
		if (HttpContext.Session.GetString("token") == null)
			return Json(new { success = false, message = "Oturum açmalısınız." });

		var response = await _api.DeleteAsync($"api/review/content/{contentId}");

		if (response)
			return Json(new { success = true, message = "Yorumunuz silindi." });

		return Json(new { success = false, message = "Silme işlemi başarısız." });
	}
}
