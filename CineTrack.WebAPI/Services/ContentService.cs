using System.Text.Json;
using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.WebAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.WebAPI.Services;

public class ContentService
{
	private readonly CineTrackDbContext _context;
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _config;

	public ContentService(CineTrackDbContext context, IConfiguration config)
	{
		_context = context;
		_config = config;
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromSeconds(10); // Timeout ekleyelim
	}

	// 1) Film arama (FİLTRELEME İLE) - DÜZELTİLDİ
	public async Task<List<ContentDto>> SearchMoviesAsync(string query, string? genre = null, string? year = null, double? minRating = null)
	{
		var apiKey = _config["ExternalApis:TMDb:ApiKey"];
		var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];

		try
		{
			// Eğer filtre varsa DISCOVER kullan, yoksa SEARCH kullan
			bool hasFilters = !string.IsNullOrEmpty(genre) || !string.IsNullOrEmpty(year) || minRating.HasValue;

			string url;
			
			if (hasFilters || string.IsNullOrWhiteSpace(query))
			{
				// DISCOVER endpoint (filtreleme için)
				url = $"{baseUrl}/discover/movie?api_key={apiKey}&language=tr-TR&sort_by=popularity.desc";
				
				// Yıl filtresi
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

				// Rating filtresi
				if (minRating.HasValue)
				{
					url += $"&vote_average.gte={minRating.Value}";
				}

				// Genre filtresi (TMDb genre ID'leri ile çalışır, şimdilik atlıyoruz)
				// Eğer genre kullanacaksanız, önce genre mapping yapmanız gerekir
			}
			else
			{
				// SEARCH endpoint (sadece query için)
				url = $"{baseUrl}/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(query)}&language=tr-TR";
			}

			var response = await _httpClient.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);

			var results = doc.RootElement.GetProperty("results");
			var contents = new List<ContentDto>();

			foreach (var item in results.EnumerateArray())
			{
				// Query ile manuel filtreleme (discover'da query parametresi yok)
				if (!string.IsNullOrWhiteSpace(query) && hasFilters)
				{
					var title = item.GetProperty("title").GetString() ?? "";
					if (!title.Contains(query, StringComparison.OrdinalIgnoreCase))
						continue;
				}

				// Client-side rating kontrolü (ekstra güvenlik için)
				if (minRating.HasValue)
				{
					var rating = item.TryGetProperty("vote_average", out var voteAvg)
						? voteAvg.GetDouble()
						: 0;

					if (rating < minRating.Value)
						continue;
				}

				contents.Add(new ContentDto
				{
					Id = item.GetProperty("id").GetInt32().ToString(),
					Title = item.GetProperty("title").GetString() ?? "Bilinmiyor",
					ContentType = "movie",
					CoverUrl = item.TryGetProperty("poster_path", out var poster)
						? $"https://image.tmdb.org/t/p/w500{poster.GetString()}"
						: null,
					Rating = item.TryGetProperty("vote_average", out var va) ? va.GetDouble() : null,
					Year = item.TryGetProperty("release_date", out var rd) && rd.GetString()?.Length >= 4
						? int.Parse(rd.GetString()!.Substring(0, 4))
						: null
				});
			}

			return contents;
		}
		catch (HttpRequestException ex)
		{
			Console.WriteLine($"SearchMoviesAsync HTTP Error: {ex.Message}");
			return new List<ContentDto>();
		}
		catch (TaskCanceledException ex)
		{
			Console.WriteLine($"SearchMoviesAsync Timeout: {ex.Message}");
			return new List<ContentDto>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SearchMoviesAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	// 2) Kitap arama (FİLTRELEME İLE) - DÜZELTİLDİ
	public async Task<List<ContentDto>> SearchBooksAsync(string query, string? genre = null, string? year = null, double? minRating = null)
	{
		var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];

		// Boş query için genel arama
		if (string.IsNullOrWhiteSpace(query))
			query = "bestseller";

		var url = $"{baseUrl}?q={Uri.EscapeDataString(query)}&maxResults=40";

		// Google Books API'si filtrelemeyi query içinde yapar
		if (!string.IsNullOrEmpty(year))
		{
			// Tek yıl veya aralık için query'ye ekle
			url += $"+subject:{year}"; // veya publishedDate parametresi kullanılabilir
		}

		try
		{
			var response = await _httpClient.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);

			IEnumerable<JsonElement> items;

			if (doc.RootElement.TryGetProperty("items", out var arr))
				items = arr.EnumerateArray();
			else
				items = Array.Empty<JsonElement>();

			var contents = new List<ContentDto>();

			foreach (var item in items)
			{
				var volumeInfo = item.GetProperty("volumeInfo");

				// Yıl kontrolü (client-side)
				if (!string.IsNullOrEmpty(year))
				{
					var publishedDate = volumeInfo.TryGetProperty("publishedDate", out var pd)
						? pd.GetString()
						: "";

					if (!string.IsNullOrEmpty(publishedDate) && publishedDate.Length >= 4)
					{
						var bookYear = int.Parse(publishedDate.Substring(0, 4));

						if (year.Contains("-"))
						{
							var parts = year.Split('-');
							var startYear = int.Parse(parts[0]);
							var endYear = int.Parse(parts[1]);

							if (bookYear < startYear || bookYear > endYear)
								continue;
						}
						else
						{
							if (bookYear != int.Parse(year))
								continue;
						}
					}
					else
					{
						continue;
					}
				}

				// Rating kontrolü
				if (minRating.HasValue)
				{
					var rating = volumeInfo.TryGetProperty("averageRating", out var ar)
						? ar.GetDouble()
						: 0;

					if (rating < minRating.Value)
						continue;
				}

				// Genre/Category kontrolü
				if (!string.IsNullOrEmpty(genre))
				{
					var hasGenre = false;
					if (volumeInfo.TryGetProperty("categories", out var categories))
					{
						foreach (var cat in categories.EnumerateArray())
						{
							if (cat.GetString()?.Contains(genre, StringComparison.OrdinalIgnoreCase) == true)
							{
								hasGenre = true;
								break;
							}
						}
					}

					if (!hasGenre)
						continue;
				}

				var id = item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
				var title = volumeInfo.GetProperty("title").GetString() ?? "Bilinmiyor";
				var cover = volumeInfo.TryGetProperty("imageLinks", out var link) && link.TryGetProperty("thumbnail", out var img)
					? img.GetString()
					: null;

				contents.Add(new ContentDto
				{
					Id = id,
					Title = title,
					ContentType = "book",
					CoverUrl = cover,
					Rating = volumeInfo.TryGetProperty("averageRating", out var avgRat) ? avgRat.GetDouble() : null,
					Year = volumeInfo.TryGetProperty("publishedDate", out var pd2) && pd2.GetString()?.Length >= 4
						? int.Parse(pd2.GetString()!.Substring(0, 4))
						: null
				});
			}

			return contents;
		}
		catch (HttpRequestException ex)
		{
			Console.WriteLine($"SearchBooksAsync HTTP Error: {ex.Message}");
			return new List<ContentDto>();
		}
		catch (TaskCanceledException ex)
		{
			Console.WriteLine($"SearchBooksAsync Timeout: {ex.Message}");
			return new List<ContentDto>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SearchBooksAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	// 3) İçerik detayını getir (önce DB'den, yoksa API'den)
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

		// Harici API'den çek
		if (type == "movie")
			return await FetchAndSaveMovieAsync(id);
		else if (type == "book")
			return await FetchAndSaveBookAsync(id);

		return null;
	}

	// 4) Film detayını getir ve kaydet
	private async Task<ContentDto?> FetchAndSaveMovieAsync(string id)
	{
		try
		{
			var apiKey = _config["ExternalApis:TMDb:ApiKey"];
			var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];
			var url = $"{baseUrl}/movie/{id}?api_key={apiKey}&language=tr-TR";

			var response = await _httpClient.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);
			var root = doc.RootElement;

			var title = root.GetProperty("title").GetString()!;
			var cover = root.TryGetProperty("poster_path", out var poster)
				? $"https://image.tmdb.org/t/p/w500{poster.GetString()}"
				: null;

			var content = new Content
			{
				Id = id,
				Title = title,
				ContentType = "movie",
				CoverUrl = cover,
				MetadataJson = response
			};

			_context.Contents.Add(content);
			await _context.SaveChangesAsync();

			return new ContentDto
			{
				Id = id,
				Title = title,
				ContentType = "movie",
				CoverUrl = cover,
				MetadataJson = response
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"FetchAndSaveMovieAsync Error: {ex.Message}");
			return null;
		}
	}

	// 5) Kitap detayını getir ve kaydet
	private async Task<ContentDto?> FetchAndSaveBookAsync(string id)
	{
		try
		{
			var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];
			var url = $"{baseUrl}/{id}";

			var response = await _httpClient.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);
			var info = doc.RootElement.GetProperty("volumeInfo");

			var title = info.GetProperty("title").GetString() ?? "Bilinmiyor";
			var cover = info.TryGetProperty("imageLinks", out var link) && link.TryGetProperty("thumbnail", out var img)
				? img.GetString()
				: null;

			var content = new Content
			{
				Id = id,
				Title = title,
				ContentType = "book",
				CoverUrl = cover,
				MetadataJson = response
			};

			_context.Contents.Add(content);
			await _context.SaveChangesAsync();

			return new ContentDto
			{
				Id = id,
				Title = title,
				ContentType = "book",
				CoverUrl = cover,
				MetadataJson = response
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"FetchAndSaveBookAsync Error: {ex.Message}");
			return null;
		}
	}
}
