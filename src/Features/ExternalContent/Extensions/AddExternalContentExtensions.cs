namespace Conesoft_Website_News.Features.ExternalContent.Extensions;

using System.Net;

static class AddExternalContentExtensions
{
    public static IApplicationBuilder MapExternalContent(this WebApplication app)
    {
        app.Map("/external/{url}", async (string url, IHttpClientFactory factory) => Results.Bytes(await factory.CreateClient().GetByteArrayAsync(WebUtility.UrlDecode(url))));
        return app;
    }
}