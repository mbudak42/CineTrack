using System.Text.Json;
using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.WebAPI.Services;

public class ContentService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _config;

	public ContentService(CineTrackDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
	{
		_context = context;
		_config = config;
		_httpClientFactory = httpClientFactory;
	}

	// --- 1) Film Arama ve Filtreleme (GÜNCELLENEN MANTIK) ---
	public async Task<List<ContentDto>> SearchMoviesAsync(string query, string? genre = null, string? year = null, double? minRating = null, int page = 1)
	{
		var apiKey = _config["ExternalApis:TMDb:ApiKey"];
		var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];
		var client = _httpClientFactory.CreateClient();
		var contents = new List<ContentDto>();

		// Türkçe tür ismini TMDB ID'sine çevir
		int? genreId = !string.IsNullOrEmpty(genre) ? GetGenreId(genre) : null;

		// Filtre var mı kontrolü
		bool hasFilters = genreId.HasValue || !string.IsNullOrEmpty(year) || minRating.HasValue;

		try
		{
			// SENARYO A: Kullanıcı bir film ismi yazdı (SEARCH MODE)
			if (!string.IsNullOrWhiteSpace(query))
			{
				// Eğer filtre varsa ve arama yapılıyorsa, tek sayfa yetmez.
				// API'den ilk 5 sayfayı (100 film) çekip havuz yapalım ki filtreleyince elimizde veri kalsın.
				// Filtre yoksa normal tek sayfa çekimi yeterli (performans için).
				int pagesToFetch = hasFilters ? 10 : 1;

				var tasks = new List<Task<string>>();
				for (int i = 1; i <= pagesToFetch; i++)
				{
					// Yıl parametresi search endpointinde "primary_release_year" olarak desteklenir (sadece tek yıl ise)
					string url = $"{baseUrl}/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(query)}&language=tr-TR&page={i}";

					if (!string.IsNullOrEmpty(year) && !year.Contains("-"))
					{
						url += $"&primary_release_year={year}";
					}

					tasks.Add(client.GetStringAsync(url));
				}

				// İstekleri paralel atıyoruz
				var responses = await Task.WhenAll(tasks);
				var allResults = new List<JsonElement>();

				foreach (var response in responses)
				{
					using var doc = JsonDocument.Parse(response);
					if (doc.RootElement.TryGetProperty("results", out var results))
					{
						foreach (var item in results.EnumerateArray())
						{
							allResults.Add(item.Clone()); // Clone önemli, yoksa disposed object hatası alabiliriz
						}
					}
				}

				// RAM ÜZERİNDE FİLTRELEME VE SAYFALAMA
				foreach (var item in allResults)
				{
					// 1. Kategori Filtresi
					if (genreId.HasValue)
					{
						var genreIds = item.GetProperty("genre_ids");
						bool match = false;
						foreach (var g in genreIds.EnumerateArray())
						{
							if (g.GetInt32() == genreId.Value) { match = true; break; }
						}
						if (!match) continue;
					}

					// 2. Puan Filtresi
					double rating = item.TryGetProperty("vote_average", out var v) ? v.GetDouble() : 0;
					if (minRating.HasValue && rating < minRating.Value) continue;

					// 3. Yıl Aralığı Filtresi (Örn: 2000-2010)
					if (!string.IsNullOrEmpty(year) && year.Contains("-"))
					{
						var dateStr = item.TryGetProperty("release_date", out var rd) ? rd.GetString() : "";
						if (string.IsNullOrEmpty(dateStr) || dateStr.Length < 4) continue;

						int movieYear = int.Parse(dateStr.Substring(0, 4));
						var parts = year.Split('-');
						if (movieYear < int.Parse(parts[0]) || movieYear > int.Parse(parts[1])) continue;
					}

					contents.Add(MapToDto(item, "movie"));
				}

				// Havuzdan ilgili sayfayı al (Skip/Take)
				// Not: Kullanıcı 6. sayfaya geçerse (100 filmden sonrası) boş dönecektir, bu kabul edilebilir bir sınır.
				return contents.Skip((page - 1) * 18).Take(18).ToList();
			}
			// SENARYO B: İsim yazılmadı, sadece filtreler seçildi (DISCOVER MODE)
			else
			{
				var url = $"{baseUrl}/discover/movie?api_key={apiKey}&language=tr-TR&sort_by=popularity.desc&page={page}";

				if (genreId.HasValue)
					url += $"&with_genres={genreId}";

				if (minRating.HasValue)
					url += $"&vote_average.gte={minRating}";

				if (!string.IsNullOrEmpty(year))
				{
					if (year.Contains("-"))
					{
						var parts = year.Split('-');
						url += $"&primary_release_date.gte={parts[0]}-01-01&primary_release_date.lte={parts[1]}-12-31";
					}
					else
					{
						url += $"&primary_release_year={year}";
					}
				}

				var response = await client.GetStringAsync(url);
				using var doc = JsonDocument.Parse(response);
				if (doc.RootElement.TryGetProperty("results", out var results))
				{
					foreach (var item in results.EnumerateArray())
					{
						contents.Add(MapToDto(item, "movie"));
					}
				}

				// Discover modunda API zaten filtrelenmiş veri döndüğü için direkt alıyoruz
				return contents.Take(18).ToList();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SearchMoviesAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	// --- 2) Kitap arama (Limit 18 olarak güncellendi) ---
	public async Task<List<ContentDto>> SearchBooksAsync(string query, string? genre = null, string? year = null, double? minRating = null, int page = 1)
	{
		var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];
		if (string.IsNullOrWhiteSpace(query)) query = "bestseller";

		int pageSize = 18; // İSTEK: 18 adet
		int startIndex = (page - 1) * pageSize;
		var url = $"{baseUrl}?q={Uri.EscapeDataString(query)}";

		if (!string.IsNullOrEmpty(genre)) url += $"+subject:{genre}";

		url += $"&startIndex={startIndex}&maxResults={pageSize}";

		try
		{
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);

			var contents = new List<ContentDto>();
			if (doc.RootElement.TryGetProperty("items", out var items))
			{
				foreach (var item in items.EnumerateArray())
				{
					var volumeInfo = item.GetProperty("volumeInfo");

					if (minRating.HasValue)
					{
						var rating = volumeInfo.TryGetProperty("averageRating", out var ar) ? ar.GetDouble() : 0;
						if (rating < minRating.Value) continue;
					}

					// Yıl filtresi (Google books subject ile kısmi destekler ama detaylı kontrol burada)
					if (!string.IsNullOrEmpty(year))
					{
						var publishedDate = volumeInfo.TryGetProperty("publishedDate", out var pd) ? pd.GetString() : "";
						if (!string.IsNullOrEmpty(publishedDate) && publishedDate.Length >= 4)
						{
							var bookYear = int.Parse(publishedDate.Substring(0, 4));
							if (year.Contains("-"))
							{
								var parts = year.Split('-');
								if (bookYear < int.Parse(parts[0]) || bookYear > int.Parse(parts[1])) continue;
							}
							else if (bookYear != int.Parse(year)) continue;
						}
						else continue;
					}

					var id = item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
					var title = volumeInfo.GetProperty("title").GetString() ?? "Bilinmiyor";
					var cover = volumeInfo.TryGetProperty("imageLinks", out var link) && link.TryGetProperty("thumbnail", out var img)
						? img.GetString() : null;

					contents.Add(new ContentDto
					{
						Id = id,
						Title = title,
						ContentType = "book",
						CoverUrl = cover,
						Rating = volumeInfo.TryGetProperty("averageRating", out var avgRat) ? avgRat.GetDouble() : null,
						Year = volumeInfo.TryGetProperty("publishedDate", out var pd2) && pd2.GetString()?.Length >= 4
							? int.Parse(pd2.GetString()!.Substring(0, 4)) : null
					});
				}
			}
			return contents;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SearchBooksAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	private int? GetGenreId(string genreName)
	{
		var genres = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Aksiyon", 28 }, { "Macera", 12 }, { "Animasyon", 16 },
			{ "Komedi", 35 }, { "Suç", 80 }, { "Belgesel", 99 },
			{ "Drama", 18 }, { "Aile", 10751 }, { "Fantastik", 14 },
			{ "Tarih", 36 }, { "Korku", 27 }, { "Müzik", 10402 },
			{ "Gizem", 9648 }, { "Romantik", 10749 }, { "Bilim Kurgu", 878 },
			{ "TV Filmi", 10770 }, { "Gerilim", 53 }, { "Savaş", 10752 },
			{ "Western", 37 }
		};
		return genres.TryGetValue(genreName, out int id) ? id : null;
	}

	private ContentDto MapToDto(JsonElement item, string type)
	{
		string title = item.TryGetProperty("title", out var t) ? t.GetString() :
					   (item.TryGetProperty("name", out var n) ? n.GetString() : "Bilinmiyor");
		string? poster = item.TryGetProperty("poster_path", out var p) ? p.GetString() : null;
		string? date = item.TryGetProperty("release_date", out var d) ? d.GetString() :
					   (item.TryGetProperty("first_air_date", out var fa) ? fa.GetString() : null);

		return new ContentDto
		{
			Id = item.GetProperty("id").GetInt32().ToString(),
			Title = title ?? "Bilinmiyor",
			ContentType = type,
			CoverUrl = !string.IsNullOrEmpty(poster) ? $"https://image.tmdb.org/t/p/w500{poster}" : null,
			Rating = item.TryGetProperty("vote_average", out var va) ? va.GetDouble() : null,
			Year = !string.IsNullOrEmpty(date) && date.Length >= 4 ? int.Parse(date.Substring(0, 4)) : null
		};
	}

	// --- Diğer mevcut metodlar (GetContentAsync, FetchAndSaveMovieAsync vb.) aynen kalmalı ---
	public async Task<ContentDto?> GetContentAsync(string id, string type)
	{
		var content = await _context.Contents.FirstOrDefaultAsync(c => c.Id == id);
		if (content != null)
		{
			return new ContentDto
			{
				Id = content.Id,
				Title = content.Title,
				ContentType = content.ContentType,
				CoverUrl = content.CoverUrl,
				MetadataJson = content.MetadataJson
			};
		}
		if (type == "movie") return await FetchAndSaveMovieAsync(id);
		else if (type == "book") return await FetchAndSaveBookAsync(id);
		return null;
	}

	private async Task<ContentDto?> FetchAndSaveMovieAsync(string id)
	{
		try
		{
			var apiKey = _config["ExternalApis:TMDb:ApiKey"];
			var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetStringAsync($"{baseUrl}/movie/{id}?api_key={apiKey}&language=tr-TR&append_to_response=credits");
			using var doc = JsonDocument.Parse(response);
			var root = doc.RootElement;
			var title = root.GetProperty("title").GetString()!;
			var cover = root.TryGetProperty("poster_path", out var poster) ? $"https://image.tmdb.org/t/p/w500{poster.GetString()}" : null;
			var content = new Content { Id = id, Title = title, ContentType = "movie", CoverUrl = cover, MetadataJson = response };
			if (!await _context.Contents.AnyAsync(c => c.Id == id)) { _context.Contents.Add(content); await _context.SaveChangesAsync(); }
			return new ContentDto { Id = id, Title = title, ContentType = "movie", CoverUrl = cover, MetadataJson = response };
		}
		catch { return null; }
	}

	private async Task<ContentDto?> FetchAndSaveBookAsync(string id)
	{
		try
		{
			var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetStringAsync($"{baseUrl}/{id}");
			using var doc = JsonDocument.Parse(response);
			var info = doc.RootElement.GetProperty("volumeInfo");
			var title = info.GetProperty("title").GetString() ?? "Bilinmiyor";
			var cover = info.TryGetProperty("imageLinks", out var link) && link.TryGetProperty("thumbnail", out var img) ? img.GetString() : null;
			var content = new Content { Id = id, Title = title, ContentType = "book", CoverUrl = cover, MetadataJson = response };
			if (!await _context.Contents.AnyAsync(c => c.Id == id)) { _context.Contents.Add(content); await _context.SaveChangesAsync(); }
			return new ContentDto { Id = id, Title = title, ContentType = "book", CoverUrl = cover, MetadataJson = response };
		}
		catch { return null; }
	}
}