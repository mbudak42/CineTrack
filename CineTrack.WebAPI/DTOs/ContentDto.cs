public class ContentDto
{
	public string Id { get; set; }
	public string Title { get; set; }
	public string ContentType { get; set; }
	public string? CoverUrl { get; set; }
	public string? MetadataJson { get; set; }
	public double? Rating { get; set; }
	public int? Year { get; set; }
}
