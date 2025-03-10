using Conesoft.Hosting;
using Conesoft.PwaGenerator;
using Conesoft_Website_News.Components;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddHostConfigurationFiles()
    .AddUsersWithStorage()
    .AddHostEnvironmentInfo()
    .AddLoggingService()
    ;

builder.Services
    .AddCompiledHashCacheBuster()
    .AddHttpClient()

    .AddRazorComponents().AddInteractiveServerComponents().AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(0);
        options.DisconnectedCircuitMaxRetained = 0;
    });

var app = builder.Build();

app.MapPwaInformationFromAppSettings();
app.MapUsersWithStorage();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
