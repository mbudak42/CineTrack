using CineTrack.MvcUI.Models;
using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.MvcUI.Controllers;

public class SearchController : Controller
{
	private readonly ApiService _api;

	public SearchController(ApiService api)
	{
		_api = api;
	}

	// GET: /Search
	public async Task<IActionResult> Index(string? query, string? contentType, string? genre, string? year, double? minRating)
	{
		var model = new SearchViewModel
		{
			Query = query,
			ContentType = contentType ?? "movie",
			Genre = genre,
			Year = year,
			MinRating = minRating
		};

		try
		{
			model.AvailableGenres = await _api.GetAsync<List<string>>("api/content/genres") ?? new List<string>();

			// Filtre veya arama varsa sonuçları getir
			if (!string.IsNullOrWhiteSpace(query) || !string.IsNullOrWhiteSpace(genre) || !string.IsNullOrWhiteSpace(year) || minRating.HasValue)
			{
				string endpoint = BuildSearchEndpoint(query, contentType, genre, year, minRating);
				var searchResults = await _api.GetAsync<List<ContentCardDto>>(endpoint);
				model.SearchResults = searchResults ?? new List<ContentCardDto>();
			}
			else
			{
				// Vitrinleri yükle
				await LoadShowcases(model);
			}
		}
		catch (Exception ex)
		{
			ViewBag.Error = "Veriler yüklenirken bir hata oluştu.";
			Console.WriteLine($"SearchController Error: {ex.Message}");
		}

		return View(model);
	}

	// AJAX: Daha fazla sonuç yükle
	[HttpGet]
	public async Task<IActionResult> LoadMoreSearch(string? query, string? contentType, string? genre, string? year, double? minRating, int page)
	{
		try
		{
			string endpoint = BuildSearchEndpoint(query, contentType, genre, year, minRating);
			endpoint += $"&page={page}";

			var results = await _api.GetAsync<List<ContentCardDto>>(endpoint);

			if (results == null || !results.Any())
				return NoContent(); // 204 döner, JS butonu gizler

			return PartialView("_ContentCardList", results);
		}
		catch
		{
			return NoContent();
		}
	}

	private string BuildSearchEndpoint(string? query, string? type, string? genre, string? year, double? minRating)
	{
		var parameters = new List<string>();

		if (!string.IsNullOrWhiteSpace(query)) parameters.Add($"q={Uri.EscapeDataString(query)}");
		if (!string.IsNullOrWhiteSpace(genre)) parameters.Add($"genre={Uri.EscapeDataString(genre)}");
		if (!string.IsNullOrWhiteSpace(year)) parameters.Add($"year={Uri.EscapeDataString(year)}");
		if (minRating.HasValue) parameters.Add($"minRating={minRating}");

		string paramString = string.Join("&", parameters);
		string endpointType = (type == "book") ? "books" : "movies";

		return $"api/content/search/{endpointType}?{paramString}";
	}

	private async Task LoadShowcases(SearchViewModel model)
	{
		try
		{
			model.TopRated = await _api.GetAsync<List<ContentCardDto>>("api/content/top-rated?limit=12") ?? new List<ContentCardDto>();
			model.MostPopular = await _api.GetAsync<List<ContentCardDto>>("api/content/most-popular?limit=12") ?? new List<ContentCardDto>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Showcase Error: {ex.Message}");
		}
	}
}