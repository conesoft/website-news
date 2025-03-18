using AngleSharp.Html.Parser;
using Conesoft.Tools;
using System.Buffers;

namespace Conesoft_Website_News.Features.RssFinder.Services;

public class RssFinderService(IHttpClientFactory factory)
{
    readonly SearchValues<string> searchValues = SearchValues.Create(["<html", "<rss"], StringComparison.OrdinalIgnoreCase);

    public async Task<string[]> FindRssFeedsOnUrl(string url, bool trymore = true)
    {
        url = url.StartsWith("https://") ? url : $"https://{url}";

        var client = factory.CreateClient();
        ReadOnlySpan<char> content = (await Safe.TryAsync(() => client.GetStringAsync(url))) ?? "";

        var found = content.IndexOfAny(searchValues) switch
        {
            -1 => [],
            int i when content[i..].StartsWith("<html") => await FindInHtmlDocument(content.ToString(), url),
            int i when content[i..].StartsWith("<rss") => [url],
            _ => []
        };

        if (trymore == true)
        {
            url = url.EndsWith('/') ? url[..^1] : url;
            found = found.Length > 0 ? found : await FindRssFeedsOnUrl(url + "/feed/", trymore = false);
            found = found.Length > 0 ? found : await FindRssFeedsOnUrl(url + "/rss.xml", trymore = false);
        }

        return found;
    }

    private static async Task<string[]> FindInHtmlDocument(string content, string url)
    {
        var document = await new HtmlParser().ParseDocumentAsync(content);
        return [.. document
            .QuerySelectorAll("[type='application/rss+xml']")
            .Select(tag => Safe.Try(() => FixIfRelativeUrl(tag.GetAttribute("href"), url)))
            .NotNull()
        ];
    }

    private static string? FixIfRelativeUrl(string? url, string domain)
    {
        if (url == null) return null;

        var src = new Uri(domain, UriKind.Absolute);
        var dst = new Uri(url, UriKind.RelativeOrAbsolute);

        return (dst.IsAbsoluteUri ? dst : new Uri(src, dst)).ToString();
    }
}