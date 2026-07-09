using System;
using Jellyfin.Plugin.SkipTime.Data;
using Jellyfin.Plugin.SkipTime.Providers;
using Jellyfin.Plugin.SkipTime.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SkipTime;

/// <summary>
/// Registers Skip Time services.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(
        IServiceCollection serviceCollection,
        IServerApplicationHost applicationHost)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);
        ArgumentNullException.ThrowIfNull(applicationHost);

        serviceCollection.AddSingleton<ISkipRepository, SkipRepository>();

        serviceCollection.AddSingleton<ISkipSegmentCache, SkipSegmentCache>();

        serviceCollection.AddSingleton<IMediaSegmentProvider, SkipSegmentProvider>();

        serviceCollection.AddHostedService<Entrypoint>();
    }
}
