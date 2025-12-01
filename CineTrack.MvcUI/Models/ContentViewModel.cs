namespace CineTrack.MvcUI.Models;

public class ContentViewModel
{
	public string Id { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty; // "movie" | "book"
	public string? CoverUrl { get; set; }
	public string? MetadataJson { get; set; }

	// Meta bilgileri
	public string? Overview { get; set; } // Özet
	public int? Year { get; set; }
	public int? DurationOrPageCount { get; set; } // Film süresi / kitap sayfa sayısı
	public List<string> Genres { get; set; } = new();
	public List<string> Directors { get; set; } = new(); // film
	public List<string> Authors { get; set; } = new();   // kitap

	// Puanlama
	public double? AverageRating { get; set; }
	public int RatingCount { get; set; }
	public int CurrentUserRating { get; set; }
	public string? CurrentUserReview { get; set; }
	public List<CommentDto> Comments { get; set; } = new();
}

public class CommentDto
{
	public string UserName { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}
