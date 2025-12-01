using System.ComponentModel.DataAnnotations;

namespace CineTrack.MvcUI.Models;

// Arama için kullanılacak ViewModel
public class SearchViewModel
{
	public string? Query { get; set; }

	public string? ContentType { get; set; } // "movie", "book", "all"

	public string? Genre { get; set; }

	public string? Year { get; set; }

	public double? MinRating { get; set; }

	public List<ContentCardDto> SearchResults { get; set; } = new();

	public List<ContentCardDto> TopRated { get; set; } = new();

	public List<ContentCardDto> MostPopular { get; set; } = new();

	public List<string> AvailableGenres { get; set; } = new();
}

// Arama sonuçları için kart modeli
public class ContentCardDto
{
	public string Id { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty;
	public string? CoverUrl { get; set; }
	public string? MetadataJson { get; set; }
	public int? Year { get; set; }
}


// API'den gelecek arama sonucu
