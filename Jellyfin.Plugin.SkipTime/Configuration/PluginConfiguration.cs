using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SkipTime.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool AutoSkipEnabled { get; set; } = true;

    public bool EnableLogging { get; set; } = false;
}