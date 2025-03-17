using Conesoft.Hosting;
using Conesoft.PwaGenerator;
using Conesoft_Website_News.Components;
using Conesoft_Website_News.Features.RssFinder.Services;

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

app.Run();
