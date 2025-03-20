using Conesoft.Hosting;
using Conesoft_Website_News.Features.Refresher.Interfaces;

namespace Conesoft_Website_News.Features.Refresher.Services;

class PeriodicContentRefresher(IContentRefresher refresher) : PeriodicTask(TimeSpan.FromMinutes(15))
{
    protected override Task Process()
    {
        refresher.BeginRefresh();
        return Task.CompletedTask;
    }
}