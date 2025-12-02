using Microsoft.AspNetCore.Mvc;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
	private readonly ContentService _contentService;

	public ContentController(ContentService contentService)
	{
		_contentService = contentService;
	}

	// 1) Film arama - FİLTRELEME PARAMETRELERİ EKLENDİ
	[HttpGet("search/movies")]
	public async Task<IActionResult> SearchMovies(
		[FromQuery] string q,
		[FromQuery] string? genre = null,
		[FromQuery] string? year = null,
		[FromQuery] double? minRating = null,
		[FromQuery] int page = 1)
	{
		if (string.IsNullOrWhiteSpace(q))
			return BadRequest(new { message = "Arama sorgusu gerekli." });

		var results = await _contentService.SearchMoviesAsync(q, genre, year, minRating, page);
		return Ok(results);
	}

	// 2) Kitap arama - FİLTRELEME PARAMETRELERİ EKLENDİ
	[HttpGet("search/books")]
	public async Task<IActionResult> SearchBooks(
		[FromQuery] string q,
		[FromQuery] string? genre = null,
		[FromQuery] string? year = null,
		[FromQuery] double? minRating = null,
		[FromQuery] int page = 1)
	{
		if (string.IsNullOrWhiteSpace(q))
			return BadRequest(new { message = "Arama sorgusu gerekli." });

		var results = await _contentService.SearchBooksAsync(q, genre, year, minRating, page);
		return Ok(results);
	}

	// 3) Detay getir
	[HttpGet("{type}/{id}")]
	public async Task<IActionResult> GetContent(string type, string id)
	{
		var content = await _contentService.GetContentAsync(id, type);
		if (content == null)
			return NotFound(new { message = "Icerik bulunamadi." });

		return Ok(content);
	}

	// 4) Türleri getir
	[HttpGet("genres")]
	public IActionResult GetGenres()
	{
		// TMDb için örnek türler
		var genres = new List<string>
		{
			"Aksiyon", "Macera", "Animasyon", "Komedi", "Suç",
			"Belgesel", "Drama", "Aile", "Fantastik", "Tarih",
			"Korku", "Müzik", "Gizem", "Romantik", "Bilim Kurgu",
			"Gerilim", "Savaş", "Western"
		};
		return Ok(genres);
	}

	// 5) En yüksek puanlılar
	[HttpGet("top-rated")]
	public async Task<IActionResult> GetTopRated([FromQuery] int limit = 12)
	{
		// Basit bir implementasyon - TMDb'nin top rated endpoint'ini kullanabilirsiniz
		var results = await _contentService.SearchMoviesAsync("", minRating: 8.0);
		return Ok(results.Take(limit));
	}

	// 6) En popülerler
	[HttpGet("most-popular")]
	public async Task<IActionResult> GetMostPopular([FromQuery] int limit = 12)
	{
		// TMDb'nin popular endpoint'ini kullanmak daha iyi olur
		// Şimdilik genel arama yapıyoruz
		var results = await _contentService.SearchMoviesAsync("popular");
		return Ok(results.Take(limit));
	}
}