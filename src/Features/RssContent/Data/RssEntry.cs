namespace Conesoft_Website_News.Features.RssContent.Data;

public record RssEntry(string Url, string Title, string Site)
{
    public string? Description { get; init; }
    public string? Image { get; init; }
};