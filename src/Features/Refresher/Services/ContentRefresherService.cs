using Conesoft.Tools;
using Conesoft_Website_News.Features.Refresher.Interfaces;
using Conesoft_Website_News.Features.RssReader.Services;
using Serilog;
using System.Threading.Channels;

namespace Conesoft_Website_News.Features.Refresher.Services;

class ContentRefresherService(RssReaderService rssReader) : BackgroundService, IContentRefresher
{
    readonly Channel<string?> queue = Channel.CreateUnbounded<string?>();

    void IContentRefresher.BeginRefresh(string? domain) => queue.Writer.WriteAsync(domain).FireAndForget();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            var domain = await queue.Reader.ReadAsync(stoppingToken);
            Log.Information("refreshing {domain}", domain ?? "everything");

            await rssReader.Process(domain);
        }
    }
}