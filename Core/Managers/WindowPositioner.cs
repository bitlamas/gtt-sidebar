using System.Windows;
using gtt_sidebar.Core.Settings;

namespace gtt_sidebar.Core.Managers
{
    /// <summary>
    /// Handles window positioning logic for the sidebar
    /// </summary>
    public static class WindowPositioner
    {
        /// <summary>
        /// Positions the sidebar window using current settings
        /// </summary>
        public static void PositionSidebarWindow(Window window)
        {
            var settings = SettingsStorage.LoadSettings();
            PositionSidebarWindow(window, settings.Window);
        }

        /// <summary>
        /// Positions the sidebar window using specified settings
        /// </summary>
        public static void PositionSidebarWindow(Window window, WindowSettings settings)
        {
            // Get screen dimensions
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;

            // Apply settings
            window.Width = settings.Width;
            window.Height = screenHeight - taskbarHeight - settings.MarginTop - settings.MarginBottom;

            // Position based on settings
            if (settings.Position == SidebarPosition.Right)
            {
                window.Left = screenWidth - settings.Width - settings.MarginSide;
            }
            else // Left
            {
                window.Left = settings.MarginSide;
            }

            window.Top = settings.MarginTop;
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            System.Diagnostics.Debug.WriteLine($"Positioned sidebar: {settings.Position}, Left={window.Left}, Width={window.Width}");
        }
    }
}