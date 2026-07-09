using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SkipTime.Configuration;

/// <summary>
/// Configuration options for the Skip Time plugin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether automatic skipping is enabled.
    /// </summary>
    public bool AutoSkipEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether plugin logging is enabled.
    /// </summary>
    public bool EnableLogging { get; set; } = false;
}
