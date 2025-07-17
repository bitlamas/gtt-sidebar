using System.Windows;

namespace gtt_sidebar.Core.Managers
{
    /// <summary>
    /// Handles window positioning logic for the sidebar
    /// </summary>
    public static class WindowPositioner
    {
        /// <summary>
        /// Positions the sidebar window on the right side of the screen with proper margins
        /// </summary>
        /// <param name="window">The window to position</param>
        public static void PositionSidebarWindow(Window window)
        {
            // Get screen dimensions
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;
            
            // Define sidebar dimensions and margins
            var windowWidth = 122;      // pixels
            var marginRight = 5;        // pixels from screen edge
            var marginTop = 5;          // pixels from top
            var marginBottom = 5;       // pixels from bottom
            
            // Calculate and set window properties
            window.Width = windowWidth;
            window.Height = screenHeight - taskbarHeight - marginTop - marginBottom;
            window.Left = screenWidth - windowWidth - marginRight;
            window.Top = marginTop;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            
            System.Diagnostics.Debug.WriteLine($"Positioned sidebar at: Left={window.Left}, Top={window.Top}, Width={window.Width}, Height={window.Height}");
        }

        /// <summary>
        /// Gets the calculated sidebar dimensions without applying them
        /// </summary>
        /// <returns>Tuple containing (left, top, width, height)</returns>
        public static (double Left, double Top, double Width, double Height) GetSidebarDimensions()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;
            
            var windowWidth = 122;
            var marginRight = 5;
            var marginTop = 5;
            var marginBottom = 5;
            
            return (
                Left: screenWidth - windowWidth - marginRight,
                Top: marginTop,
                Width: windowWidth,
                Height: screenHeight - taskbarHeight - marginTop - marginBottom
            );
        }
    }
}