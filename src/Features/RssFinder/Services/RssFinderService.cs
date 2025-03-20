using AngleSharp.Html.Parser;
using CodeHollow.FeedReader;
using Conesoft.Tools;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Conesoft_Website_News.Features.RssFinder.Services;

public class RssFinderService(IHttpClientFactory factory)
{
    public record RssFeed(string Url, string Title, string Site)
    {
        public string? Description { get; init; }
        public string? Image { get; init; }
    };

    public async IAsyncEnumerable<RssFeed> FindRssFeedsOnUrl(string url, [EnumeratorCancellation] CancellationToken cancellation)
    {
        url = FixUrl(url, removescheme: true, fixpath: true);

        Queue<string> links = new([.. GetKnownUrlsFor(url), .. GetKnownUrlsFor("www." + url)]);
        HashSet<string> visited = [];
        string? image = null;

        while (links.Count > 0 && cancellation.IsCancellationRequested == false)
        {
            var _links = links.ToArray();
            links.Clear();
            var tasks = _links.Select(async current =>
            {
                var client = factory.CreateClient();

                current = FixUrl(current, addscheme: true);

                var content = (await Safe.TryAsync(() => client.GetStringAsync(current, cancellation))) ?? "";

                var _image = await FaviconInHtml(content, url, client);
                lock (this)
                {
                    image ??= _image;
                }

                if (await IsRssFeed(current, content) is string feed && visited.Contains(feed) == false && visited.Contains(content) == false)
                {
                    lock (this)
                    {
                        visited.Add(feed);
                        visited.Add(content);
                    }

                    var details = FeedReader.ReadFromString(content);

                    return new RssFeed(Url: feed, details.Title, Site: details.Link)
                    {
                        Description = details.Description,
                        Image = FixIfRelativeUrl(details.ImageUrl, url) ?? image
                    };
                }
                else
                {
                    foreach (var link in (await LinksInHtml(content, current)).Where(link => visited.Contains(link) == false))
                    {
                        lock (this)
                        {
                            visited.Add(current);
                            links.Enqueue(link);
                        }
                    }
                }
                return null;
            });

            await foreach(var task in Task.WhenEach(tasks))
            {
                if(await task is RssFeed feed)
                {
                    yield return feed;
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

    static async Task<string?> FaviconInHtml(string content, string url, HttpClient client)
    {
        var document = await new HtmlParser().ParseDocumentAsync(content);
        var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
        return
            await CheckIfUrlAvailable(FixIfRelativeUrl(document.QuerySelector("meta[content][property='og:image']")?.GetAttribute("content"), url), client, timeout)
            ??
            await CheckIfUrlAvailable(FixIfRelativeUrl(document.QuerySelector("link[href$='.svg'][rel*='icon']")?.GetAttribute("href"), url), client, timeout)
            ??
            await CheckIfUrlAvailable(FixIfRelativeUrl(document.QuerySelector("link[href][rel*='icon']")?.GetAttribute("href"), url), client, timeout)
            ??
            await CheckIfUrlAvailable(url + "/favicon.ico", client, timeout)
            ;
    }

    static bool IsSameHost(string child, string parent)
    {
        var _child = new Uri(child, UriKind.Absolute).Host.Replace("www.", "");
        var _parent = new Uri(parent, UriKind.Absolute).Host.Replace("www.", "");
        return _child == _parent || _child.Contains($".{_parent}");
    }

    static string FixUrl(string url, bool addscheme = false, bool removescheme = false, bool fixpath = false)
    {
        if (addscheme)
        {
            url = url.StartsWith("http://") ? url["http://".Length..] : url;
            url = url.StartsWith("https://") ? url : $"https://{url}";
        }
        if (removescheme)
        {
            url = url.StartsWith("http://") ? url["http://".Length..] : url;
            url = url.StartsWith("https://") ? url["https://".Length..] : url;
        }
        if (fixpath)
        {
            url = url.EndsWith('/') ? url[..^1] : url;
        }
        return url;
    }

    static async Task<string?> CheckIfUrlAvailable(string? url, HttpClient client, CancellationToken token)
    {
        if (url == null) return null;

        var response = await Safe.TryAsync(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), token));
        return response?.IsSuccessStatusCode ?? false ? url : null;
    }

    static string? FixIfRelativeUrl(string? url, string domain)
    {
        if (url == null) return null;

        var src = new Uri(FixUrl(domain, addscheme: true), UriKind.Absolute);
        var dst = new Uri(url, UriKind.RelativeOrAbsolute);

        if (dst.IsAbsoluteUri && dst.HostNameType != UriHostNameType.Dns) return domain;

        return (dst.IsAbsoluteUri ? dst : new Uri(src, dst)).ToString();
    }
}