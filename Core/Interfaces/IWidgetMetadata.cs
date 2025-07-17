using System;

namespace gtt_sidebar.Core.Interfaces
{
    /// <summary>
    /// Interface for widget metadata and discovery information
    /// </summary>
    public interface IWidgetMetadata
    {
        /// <summary>
        /// Display name of the widget
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the widget does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Widget author/creator name
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Widget version
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// List of dependencies required by this widget
        /// </summary>
        string[] Dependencies { get; }
    }
}