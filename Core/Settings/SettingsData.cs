using System;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Main settings data container
    /// </summary>
    public class SettingsData
    {
        public WindowSettings Window { get; set; } = new WindowSettings();
        public string Version { get; set; } = "1.0.0"; // For future migration support
    }

    /// <summary>
    /// Window positioning and sizing settings
    /// </summary>
    public class WindowSettings
    {
        public SidebarPosition Position { get; set; } = SidebarPosition.Right;
        public double Width { get; set; } = 122;
        public double MarginTop { get; set; } = 5;
        public double MarginBottom { get; set; } = 5;
        public double MarginSide { get; set; } = 5;

        /// <summary>
        /// Validates that all settings are within reasonable bounds
        /// </summary>
        public bool IsValid()
        {
            return Width >= 100 && Width <= 200 &&
                   MarginTop >= 0 && MarginTop <= 50 &&
                   MarginBottom >= 0 && MarginBottom <= 50 &&
                   MarginSide >= 0 && MarginSide <= 50;
        }

        /// <summary>
        /// Ensures values are within valid ranges, correcting if necessary
        /// </summary>
        public void Validate()
        {
            Width = Math.Max(100, Math.Min(200, Width));
            MarginTop = Math.Max(0, Math.Min(50, MarginTop));
            MarginBottom = Math.Max(0, Math.Min(50, MarginBottom));
            MarginSide = Math.Max(0, Math.Min(50, MarginSide));
        }

        /// <summary>
        /// Checks if the window would fit on screen with current settings
        /// </summary>
        public bool FitsOnScreen()
        {
            var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = System.Windows.SystemParameters.PrimaryScreenHeight -
                              System.Windows.SystemParameters.WorkArea.Height;

            var requiredHeight = MarginTop + MarginBottom + 100; // Minimum content height
            var availableHeight = screenHeight - taskbarHeight;

            return requiredHeight <= availableHeight;
        }
    }

    /// <summary>
    /// Sidebar position options
    /// </summary>
    public enum SidebarPosition
    {
        Right,
        Left
    }
}