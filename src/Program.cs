using Conesoft.Hosting;
using Conesoft.PwaGenerator;
using Conesoft_Website_News.Components;
using Conesoft_Website_News.Features.ExternalContent.Extensions;
using Conesoft_Website_News.Features.Refresher.Extensions;
using Conesoft_Website_News.Features.RssFinder.Services;
using Conesoft_Website_News.Features.RssReader.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddHostConfigurationFiles()
    .AddUsersWithStorage()
    .AddHostEnvironmentInfo()
    .AddLoggingService()
    .AddCompiledHashCacheBuster()
    .AddHostingDefaults()

    .AddContentRefresher()
    .AddRssReaderService()
    ;

builder.Services
    .AddSingleton<RssFinderService>()
    ;

var app = builder.Build();

app.UseCompiledHashCacheBuster();

app.MapPwaInformationFromAppSettings();
app.MapUsersWithStorage();
app.MapStaticAssets();
app.MapExternalContent();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
