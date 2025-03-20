namespace Conesoft_Website_News.Features.Refresher.Interfaces;

interface IContentRefresher
{
    void BeginRefresh(string? domain = null);
}