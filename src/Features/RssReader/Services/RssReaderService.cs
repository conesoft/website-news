using CodeHollow.FeedReader;
using Conesoft.Files;
using Conesoft.Hosting;
using Conesoft_Website_News.Features.RssContent.Data;
using Serilog;

namespace Conesoft_Website_News.Features.RssReader.Services;

public class RssReaderService(HostEnvironment environment)
{
    public async Task Process(string? url)
    {
        var feedStorage = environment.Global.Storage / "news" / "feeds";
        var contentStorage = environment.Global.Storage / "news" / "content";

        var sources = await feedStorage.AllFiles.ReadFromJson<RssFeed>();

        foreach (var source in sources.Where(s => url == null || s.Content.Site == url).OrderByDescending(f => f.Info.CreationTime))
        {
            Log.Information("processing feed {feed}", source.Content.Title);
            var feed = await FeedReader.ReadAsync(source.Content.Url);
            foreach(var item in feed.Items)
            {
            }
        }
    }
}