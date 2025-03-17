using Conesoft.Tools;
using HtmlAgilityPack;
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
            int i when content[i..].StartsWith("<html") => FindInHtmlDocument(content, url),
            int i when content[i..].StartsWith("<rss") => [url],
            _ => []
        };

        if(trymore == true)
        {
            url = url.EndsWith('/') ? url[..^1] : url;
            found = found.Length > 0 ? found : await FindRssFeedsOnUrl(url + "/feed/", trymore = false);
            found = found.Length > 0 ? found : await FindRssFeedsOnUrl(url + "/rss.xml", trymore = false);
        }

        return found;
    }

    private static string[] FindInHtmlDocument(ReadOnlySpan<char> content, string url)
    {
        var document = new HtmlDocument();
        document.LoadHtml(content.ToString());

        return [.. document.DocumentNode
            .Descendants("link")
            .Where(n => n.Attributes["type"] != null && n.Attributes["type"].Value == "application/rss+xml")
            .Select(n => Safe.Try(() => FixIfRelativeUrl(n.Attributes["href"].Value, url)))
            .NotNull()
        ];
    }

    private static string FixIfRelativeUrl(string url, string domain)
    {
        var src = new Uri(domain, UriKind.Absolute);
        var dst = new Uri(url, UriKind.RelativeOrAbsolute);
        return dst.IsAbsoluteUri switch
        {
            false => new UriBuilder(src.Scheme, src.Host, src.Port, url).Uri.ToString(),
            true => dst.ToString()
        };
    }
}
