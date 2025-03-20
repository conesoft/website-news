using Conesoft_Website_News.Features.Refresher.Interfaces;
using Conesoft_Website_News.Features.Refresher.Services;

namespace Conesoft_Website_News.Features.Refresher.Extensions;

static class AddContentRefresherExtensions
{
    public static WebApplicationBuilder AddContentRefresher(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ContentRefresherService>();
        builder.Services.AddHostedService(s => s.GetRequiredService<ContentRefresherService>());
        builder.Services.AddSingleton<IContentRefresher>(s => s.GetRequiredService<ContentRefresherService>());
        builder.Services.AddHostedService<PeriodicContentRefresher>();
        return builder;
    }
}