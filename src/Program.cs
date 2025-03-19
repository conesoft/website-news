using Conesoft.Hosting;
using Conesoft.PwaGenerator;
using Conesoft_Website_News.Components;
using Conesoft_Website_News.Features.RssFinder.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddHostConfigurationFiles()
    .AddUsersWithStorage()
    .AddHostEnvironmentInfo()
    .AddLoggingService()
    .AddCompiledHashCacheBuster()
    .AddHostingDefaults()
    ;

builder.Services
    .AddSingleton<RssFinderService>()
    ;

var app = builder.Build();

app.UseCompiledHashCacheBuster();

app.MapPwaInformationFromAppSettings();
app.MapUsersWithStorage();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Map("/external/{url}", async (string url, IHttpClientFactory factory) => Results.Bytes(await factory.CreateClient().GetByteArrayAsync(WebUtility.UrlDecode(url))));

app.Run();
