using AngleSharp.Html.Parser;
using CodeHollow.FeedReader;
using Conesoft.Tools;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Conesoft_Website_News.Features.RssFinder.Services;

public class RssFinderService(IHttpClientFactory factory)
{
    public record RssFeed(string Url, string Title, string Site)
    {
        public string? Description { get; init; }
        public string? Image { get; init; }
    };

    [SuppressMessage("Style", "IDE0306", Justification = "Lol Bug")]
    public async IAsyncEnumerable<RssFeed> FindRssFeedsOnUrl(string url, [EnumeratorCancellation] CancellationToken cancellation)
    {
        url = FixUrl(url);

        Queue<string> links = new(GetKnownUrlsFor(url));
        HashSet<string> visited = [];
        string? image = null;

        var client = factory.CreateClient();

        while (links.Count > 0 && cancellation.IsCancellationRequested == false)
        {
            var current = links.Dequeue();

            var content = (await Safe.TryAsync(() => client.GetStringAsync(current, cancellation))) ?? "";

            image ??= await FaviconInHtml(content, url);

            if (await IsRssFeed(current, content) is string feed && visited.Contains(feed) == false && visited.Contains(content) == false)
            {
                visited.Add(feed);
                visited.Add(content);

                var details = FeedReader.ReadFromString(content);

                yield return new RssFeed(Url: feed, details.Title, Site: details.Link)
                {
                    Description = details.Description,
                    Image = details.ImageUrl ?? image
                };
            }
            else
            {
                foreach (var link in (await LinksInHtml(content, current)).Where(link => visited.Contains(link) == false))
                {
                    visited.Add(current);
                    links.Enqueue(link);
                }
            }

        }
    }

    static IEnumerable<string> GetKnownUrlsFor(string url) => [url, url + "/rss", url + "/rss/", url + "/feed", url + "/feed/", url + "/rss.xml", url + "/feed.xml", url + "/blog", url + "/blog/"];

    readonly static SearchValues<string> rssSearchValues = SearchValues.Create(["<rss", "<feed", "<?xml", "<rdf"], StringComparison.OrdinalIgnoreCase);

    static async Task<string?> IsRssFeed(string url, string content)
    {
        if (content.AsSpan().ContainsAny(rssSearchValues) == false) return null;
        var document = await new HtmlParser().ParseDocumentAsync(content);
        return document.QuerySelectorAll("entry, item").Length > 0 ? url : null;
    }

    static async Task<string[]> LinksInHtml(string content, string url)
    {
        var document = await new HtmlParser().ParseDocumentAsync(content);
        return [.. document
            .QuerySelectorAll("link[href][type='application/rss+xml'],link[href][type='application/atom+xml'],a[href*='/rss']")
            .Select(tag => {
                return FixIfRelativeUrl(tag.GetAttribute("href"), url);
            })
            .NotNull()
            .Where(link => IsSameHost(link, url))
            .DistinctBy(url => new Uri(url).AbsolutePath)
        ];
    }

    static async Task<string?> FaviconInHtml(string content, string url)
    {
        var document = await new HtmlParser().ParseDocumentAsync(content);
        return
            FixIfRelativeUrl(document.QuerySelector("meta[content][property='og:image']")?.GetAttribute("content"), url)
            ??
            FixIfRelativeUrl(document.QuerySelector("link[href$='.svg'][rel*='icon']")?.GetAttribute("href"), url)
            ??
            FixIfRelativeUrl(document.QuerySelector("link[href][rel*='icon']")?.GetAttribute("href"), url)
            ;
    }

    static bool IsSameHost(string child, string parent)
    {
        var _child = new Uri(child, UriKind.Absolute).Host.Replace("www.", "");
        var _parent = new Uri(parent, UriKind.Absolute).Host.Replace("www.", "");
        return _child == _parent || _child.Contains($".{_parent}");
    }

    static string FixUrl(string url)
    {
        url = url.StartsWith("http://") ? url["http://".Length..] : url;
        url = url.StartsWith("https://") ? url : $"https://{url}";
        url = url.EndsWith('/') ? url[..^1] : url;
        return url;
    }

    static string? FixIfRelativeUrl(string? url, string domain)
    {
        if (url == null) return null;

        var src = new Uri(domain, UriKind.Absolute);
        var dst = new Uri(url, UriKind.RelativeOrAbsolute);

        if (dst.IsAbsoluteUri && dst.HostNameType != UriHostNameType.Dns) return domain;

        return (dst.IsAbsoluteUri ? dst : new Uri(src, dst)).ToString();
    }
}