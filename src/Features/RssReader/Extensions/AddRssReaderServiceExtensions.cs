using Conesoft_Website_News.Features.RssReader.Services;

namespace Conesoft_Website_News.Features.RssReader.Extensions;

static class AddRssReaderServiceExtensions
{
    public static WebApplicationBuilder AddRssReaderService(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<RssReaderService>();
        return builder;
    }
}