using AngleSharp.Html.Parser;
using Conesoft.Tools;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Conesoft_Website_News.Features.RssFinder.Services;

public class RssFinderService(IHttpClientFactory factory)
{
    public async IAsyncEnumerable<string> FindRssFeedsOnUrl(string url, [EnumeratorCancellation] CancellationToken cancellation)
    {
        url = FixUrl(url);

        Queue<string> links = new([url, url + "/rss", url + "/rss/", url + "/feed", url + "/feed/", url + "/rss.xml", url + "/feed.xml"]);
        HashSet<string> visited = [];

        var client = factory.CreateClient();

        while (links.Count > 0 && cancellation.IsCancellationRequested == false)
        {
            var current = links.Dequeue();
            visited.Add(current);

            var content = (await Safe.TryAsync(() => client.GetStringAsync(current, cancellation))) ?? "";

            if (IsRssFeed(content))
            {
                yield return current;
            }
            else
            {
                foreach (var link in (await LinksInHtml(content, current)).Where(link => visited.Contains(link) == false))
                {
                    links.Enqueue(link);
                }
            }

        }
    }

    readonly static SearchValues<string> rssSearchValues = SearchValues.Create(["<rss", "<feed", "<?xml"], StringComparison.OrdinalIgnoreCase);
    static bool IsRssFeed(ReadOnlySpan<char> content) => content.ContainsAny(rssSearchValues);

    static async Task<string[]> LinksInHtml(string content, string url)
    {
        var document = await new HtmlParser().ParseDocumentAsync(content);
        return [.. document
            .QuerySelectorAll("link[href][type='application/rss+xml'],a[href*='/rss']")
            .Select(tag => FixIfRelativeUrl(tag.GetAttribute("href"), url))
            .NotNull()
            .Where(link => IsSameHost(link, url))
            .DistinctBy(url => new Uri(url).AbsolutePath)
        ];
    }

    static bool IsSameHost(string uri1, string uri2) => new Uri(uri1, UriKind.Absolute).Host.Replace("www.", "") == new Uri(uri2, UriKind.Absolute).Host.Replace("www.", "");

    static string FixUrl(string url)
    {
        url = url.StartsWith("https://") ? url : $"https://{url}";
        url = url.EndsWith('/') ? url[..^1] : url;
        return url;
    }

    static string? FixIfRelativeUrl(string? url, string domain)
    {
        if (url == null) return null;

        var src = new Uri(domain, UriKind.Absolute);
        var dst = new Uri(url, UriKind.RelativeOrAbsolute);

        return (dst.IsAbsoluteUri ? dst : new Uri(src, dst)).ToString();
    }
}