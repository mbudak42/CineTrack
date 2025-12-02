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

	// 1) Film arama
	public async Task<List<ContentDto>> SearchMoviesAsync(string query, string? genre = null, string? year = null, double? minRating = null, int page = 1)
	{
		var apiKey = _config["ExternalApis:TMDb:ApiKey"];
		var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];

		try
		{
			// Client'ı factory'den üretiyoruz
			var client = _httpClientFactory.CreateClient();

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
						url = $"{baseUrl}/discover/movie?api_key={apiKey}&language=tr-TR&sort_by=popularity.desc&page={page}";
					}
					else
					{
						url = $"{baseUrl}/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(query)}&language=tr-TR&page={page}";
					}
				}

				// Rating filtresi
				if (minRating.HasValue)
				{
					url += $"&vote_average.gte={minRating.Value}";
				}
			}
			else
			{
				// SEARCH endpoint (sadece query için)
				url = $"{baseUrl}/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(query)}&language=tr-TR";
			}

			var response = await client.GetStringAsync(url);
			using var doc = JsonDocument.Parse(response);
			var results = doc.RootElement.GetProperty("results");
			var contents = new List<ContentDto>();

			foreach (var item in results.EnumerateArray())
			{
				// Query ile manuel filtreleme (discover'da query parametresi yoksa)
				if (!string.IsNullOrWhiteSpace(query) && hasFilters)
				{
					var title = item.GetProperty("title").GetString() ?? "";
					if (!title.Contains(query, StringComparison.OrdinalIgnoreCase))
						continue;
				}

				// Client-side rating kontrolü
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
		catch (Exception ex)
		{
			Console.WriteLine($"SearchMoviesAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	// 2) Kitap arama
	public async Task<List<ContentDto>> SearchBooksAsync(string query, string? genre = null, string? year = null, double? minRating = null, int page = 1)
	{
		var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];

		if (string.IsNullOrWhiteSpace(query))
			query = "bestseller";

		int pageSize = 24;
		int startIndex = (page - 1) * pageSize;
		var url = $"{baseUrl}?q={Uri.EscapeDataString(query)}&startIndex={startIndex}&maxResults={pageSize}";

		if (!string.IsNullOrEmpty(year))
		{
			url += $"+subject:{year}";
		}

		try
		{
			// Client'ı factory'den üretiyoruz
			var client = _httpClientFactory.CreateClient();

			var response = await client.GetStringAsync(url);
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

				// Yıl kontrolü
				if (!string.IsNullOrEmpty(year))
				{
					var publishedDate = volumeInfo.TryGetProperty("publishedDate", out var pd) ? pd.GetString() : "";

					if (!string.IsNullOrEmpty(publishedDate) && publishedDate.Length >= 4)
					{
						var bookYear = int.Parse(publishedDate.Substring(0, 4));

						if (year.Contains("-"))
						{
							var parts = year.Split('-');
							var startYear = int.Parse(parts[0]);
							var endYear = int.Parse(parts[1]);

							if (bookYear < startYear || bookYear > endYear) continue;
						}
						else
						{
							if (bookYear != int.Parse(year)) continue;
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
					var rating = volumeInfo.TryGetProperty("averageRating", out var ar) ? ar.GetDouble() : 0;
					if (rating < minRating.Value) continue;
				}

				// Genre kontrolü
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
					if (!hasGenre) continue;
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
		catch (Exception ex)
		{
			Console.WriteLine($"SearchBooksAsync Error: {ex.Message}");
			return new List<ContentDto>();
		}
	}

	// 3) İçerik detayı getir
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

		if (type == "movie")
			return await FetchAndSaveMovieAsync(id);
		else if (type == "book")
			return await FetchAndSaveBookAsync(id);

		return null;
	}

	// 4) Film detayı (API'den)
	private async Task<ContentDto?> FetchAndSaveMovieAsync(string id)
	{
		try
		{
			var apiKey = _config["ExternalApis:TMDb:ApiKey"];
			var baseUrl = _config["ExternalApis:TMDb:BaseUrl"];
			var url = $"{baseUrl}/movie/{id}?api_key={apiKey}&language=tr-TR";

			// Client'ı factory'den üretiyoruz
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetStringAsync(url);

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

	// 5) Kitap detayı (API'den)
	private async Task<ContentDto?> FetchAndSaveBookAsync(string id)
	{
		try
		{
			var baseUrl = _config["ExternalApis:GoogleBooks:BaseUrl"];
			var url = $"{baseUrl}/{id}";

			// Client'ı factory'den üretiyoruz
			var client = _httpClientFactory.CreateClient();
			var response = await client.GetStringAsync(url);

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