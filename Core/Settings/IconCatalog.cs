using System.Collections.Generic;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Catalog of built-in icons available for shortcuts
    /// </summary>
    public static class IconCatalog
    {
        /// <summary>
        /// All built-in icons available for shortcuts (30 total)
        /// </summary>
        public static readonly List<string> BuiltInIcons = new List<string>
        {
            // System & Tools
            "⚙️", // Settings/Config
            "🔧", // Tools
            "🖥️", // Computer/Desktop
            "💾", // Save/Storage
            "📁", // Folder
            "📋", // Clipboard
            "🔳", // Command Prompt 

            
            // Productivity & Office
            "📊", // Charts/Analytics
            "📝", // Notes/Text
            "📅", // Calendar
            "💼", // Business/Work
            "📎", // Attachments
            "🧮", // Calculator (better icon)

            
            // Internet & Communication
            "🌐", // Web/Internet
            "📧", // Email
            "💬", // Chat/Messages
            "📱", // Mobile/Apps
            "🔗", // Links
            "📡", // Network/Wireless
            
            // Media & Creative
            "🎵", // Music
            "🎬", // Video/Movies
            "📸", // Camera/Photos
            "🎨", // Art/Design
            "🎭", // Creative/Theater
            "🎯", // Target/Goals
            
            // Games & Entertainment
            "🎮", // Gaming
            "🎲", // Games/Dice
            "🃏", // Cards
            "🏆", // Achievement/Trophy
            "⚡"  // Power/Energy
        };

        /// <summary>
        /// Gets a random icon from the catalog
        /// </summary>
        public static string GetRandomIcon()
        {
            var random = new System.Random();
            return BuiltInIcons[random.Next(BuiltInIcons.Count)];
        }

        /// <summary>
        /// Checks if an icon is in the built-in catalog
        /// </summary>
        public static bool IsBuiltInIcon(string icon)
        {
            return !string.IsNullOrWhiteSpace(icon) && BuiltInIcons.Contains(icon);
        }

        /// <summary>
        /// Gets the default icon for new shortcuts
        /// </summary>
        public static string GetDefaultIcon()
        {
            return "📁"; // Folder icon as default
        }

        /// <summary>
        /// Gets an appropriate icon suggestion based on shortcut path/name
        /// </summary>
        public static string SuggestIcon(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(label))
                return GetDefaultIcon();

            var searchText = $"{path} {label}".ToLowerInvariant();

            // Simple icon suggestions based on keywords
            if (searchText.Contains("calc") || searchText.Contains("calculator"))
                return "🔧";
            else if (searchText.Contains("explorer") || searchText.Contains("folder"))
                return "📁";
            else if (searchText.Contains("paint") || searchText.Contains("art"))
                return "🎨";
            else if (searchText.Contains("cmd") || searchText.Contains("command") || searchText.Contains("terminal"))
                return "⚫";
            else if (searchText.Contains("browser") || searchText.Contains("chrome") || searchText.Contains("firefox") || searchText.Contains("edge"))
                return "🌐";
            else if (searchText.Contains("mail") || searchText.Contains("email") || searchText.Contains("outlook"))
                return "📧";
            else if (searchText.Contains("music") || searchText.Contains("spotify") || searchText.Contains("itunes"))
                return "🎵";
            else if (searchText.Contains("video") || searchText.Contains("vlc") || searchText.Contains("media"))
                return "🎬";
            else if (searchText.Contains("game") || searchText.Contains("steam") || searchText.Contains("gaming"))
                return "🎮";
            else if (searchText.Contains("photo") || searchText.Contains("image") || searchText.Contains("camera"))
                return "📸";
            else if (searchText.Contains("note") || searchText.Contains("text") || searchText.Contains("editor"))
                return "📝";
            else if (searchText.Contains("office") || searchText.Contains("word") || searchText.Contains("excel"))
                return "💼";
            else if (searchText.Contains("code") || searchText.Contains("visual studio") || searchText.Contains("dev"))
                return "⚡";
            else if (searchText.Contains("http") || searchText.Contains("www"))
                return "🌐";

            return GetDefaultIcon();
        }
    }
}