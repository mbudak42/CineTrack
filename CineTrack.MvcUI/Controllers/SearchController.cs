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
			// Türleri yükle
			model.AvailableGenres =
				await _api.GetAsync<List<string>>("api/content/genres")
				?? new List<string>();

			// Arama yapıldıysa
			if (!string.IsNullOrWhiteSpace(query))
			{
				string endpoint = BuildSearchEndpoint(query, contentType, genre, year, minRating);

				var searchResults = await _api.GetAsync<List<ContentCardDto>>(endpoint);

				model.SearchResults = searchResults ?? new List<ContentCardDto>();
			}
			else
			{
				// Arama yapılmadıysa vitrinleri göster
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

	// AJAX ile arama sonuçlarını getir
	[HttpGet]
	public async Task<IActionResult> SearchPartial(string query, string? contentType, string? genre, string? year, double? minRating)
	{
		try
		{
			string endpoint = BuildSearchEndpoint(query, contentType, genre, year, minRating);

			var searchResults = await _api.GetAsync<List<ContentCardDto>>(endpoint);

			return PartialView("_SearchResults", searchResults ?? new List<ContentCardDto>());
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SearchPartial Error: {ex.Message}");
			return PartialView("_SearchResults", new List<ContentCardDto>());
		}
	}

	// Vitrinleri yükle (En Yüksek Puanlılar, En Popülerler)
	private async Task LoadShowcases(SearchViewModel model)
	{
		try
		{
			model.TopRated =
				await _api.GetAsync<List<ContentCardDto>>("api/content/top-rated?limit=12")
				?? new List<ContentCardDto>();

			model.MostPopular =
				await _api.GetAsync<List<ContentCardDto>>("api/content/most-popular?limit=12")
				?? new List<ContentCardDto>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Showcase Error: {ex.Message}");
		}
	}

	// Arama endpointini oluştur
	private string BuildSearchEndpoint(string query, string? type, string? genre, string? year, double? minRating)
	{
		var parameters = new List<string>
	{
		$"q={Uri.EscapeDataString(query)}"
	};

		if (!string.IsNullOrWhiteSpace(genre))
			parameters.Add($"genre={Uri.EscapeDataString(genre)}");

		// year hem tek yil hem aralik olabilir
		if (!string.IsNullOrWhiteSpace(year))
			parameters.Add($"year={Uri.EscapeDataString(year)}");

		if (minRating.HasValue)
			parameters.Add($"minRating={minRating}");

		string paramString = string.Join("&", parameters);

		return type == "book"
			? $"api/content/search/books?{paramString}"
			: $"api/content/search/movies?{paramString}";
	}

}
