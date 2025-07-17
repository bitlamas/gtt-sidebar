using System.Collections.Generic;
using gtt_sidebar.Core.Interfaces;

namespace gtt_sidebar.Core.Interfaces
{
    /// <summary>
    /// Interface for widgets that support configuration/settings
    /// </summary>
    public interface IConfigurableWidget : IWidget
    {
        /// <summary>
        /// Indicates whether this widget has configurable settings
        /// </summary>
        bool HasSettings { get; }

        /// <summary>
        /// Gets the default settings for this widget
        /// </summary>
        /// <returns>Dictionary of setting keys and default values</returns>
        Dictionary<string, object> GetDefaultSettings();

        /// <summary>
        /// Loads settings into the widget
        /// </summary>
        /// <param name="settings">Dictionary of setting keys and values</param>
        void LoadSettings(Dictionary<string, object> settings);

        /// <summary>
        /// Saves current widget settings
        /// </summary>
        /// <returns>Dictionary of setting keys and current values</returns>
        Dictionary<string, object> SaveSettings();
    }
}