using System.Collections.Generic;
using System.Linq;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Catalog of built-in Phosphor icons available for shortcuts
    /// </summary>
    public static class IconCatalog
    {
        /// <summary>
        /// All built-in Phosphor icons available for shortcuts (50 total)
        /// Organized by categories for better user experience
        /// </summary>
        public static readonly List<string> BuiltInIcons = new List<string>
        {
            // Default icons (always available)
            "folder-open",      // File Explorer
            "calculator",       // Calculator
            "palette",          // MSPaint
            "terminal-window",  // Command Prompt
            
            // Development & Code
            "database",         // DBeaver, SQL tools
            "github-logo",      // GitHub Desktop
            "file-arrow-up",    // FileZilla, FTP
            "note-pencil",      // Notepad++, text editors
            "brackets-curly",   // Code editors
            "dev-to-logo",      // Development community
            "hash",             // Programming symbols
            "package",          // Package managers
            "stack",            // Technology stack
            "open-ai-logo",     // AI development tools
            
            // Communication & Social
            "envelope",         // Email applications
            "chat-text",        // Chat applications
            "discord-logo",     // Discord
            "slack-logo",       // Slack
            "microsoft-teams-logo", // Microsoft Teams
            "messenger-logo",   // Facebook Messenger
            
            // Microsoft Office Suite
            "microsoft-excel-logo",   // Excel
            "microsoft-outlook-logo", // Outlook
            "microsoft-word-logo",    // Word
            
            // System & Tools
            "app-window",       // Applications
            "archive",          // Archive tools (7-Zip, WinRAR)
            "desktop",          // Desktop/system tools
            "wrench",           // System utilities
            "pulse",            // System monitoring
            
            // Creative & Design
            "bookmarks",        // Bookmark managers
            "checkerboard",     // Design patterns
            "image",            // Image editors (GIMP, Photoshop)
            "puzzle-piece",     // Puzzle games, problem solving
            
            // Network & Web
            "link-simple",      // URL shortcuts
            "globe",            // Web browsers
            "cell-signal-full", // Network tools
            "cloud",            // Cloud storage
            "export",           // Download managers
            
            // Finance & Analytics
            "currency-btc",     // Bitcoin tools
            "currency-dollar",  // Financial apps
            "money",            // Banking, finance
            "chart-line",       // Analytics tools
            
            // Gaming & Entertainment
            "joystick",         // Gaming applications
            "steam-logo",       // Steam gaming platform
            "traffic-cone",     // VLC media player
            
            // Miscellaneous
            "planet",           // Astronomy, mapping
            "potted-plant",     // Lifestyle apps
            "question",         // Help, support tools
            "rocket-launch",    // Startup apps, fast tools
            "confetti",         // Fun applications
            "user"              // User profile, account tools
        };

        /// <summary>
        /// Gets a specific icon by name (case-insensitive)
        /// </summary>
        public static string GetIcon(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
                return GetDefaultIcon();

            var icon = BuiltInIcons.FirstOrDefault(i =>
                i.Equals(iconName, System.StringComparison.OrdinalIgnoreCase));

            return icon ?? GetDefaultIcon();
        }

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
            return !string.IsNullOrWhiteSpace(icon) &&
                   BuiltInIcons.Contains(icon, System.StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the default icon for new shortcuts
        /// </summary>
        public static string GetDefaultIcon()
        {
            return "folder-open"; // Default to folder-open icon
        }

        /// <summary>
        /// Gets an appropriate icon suggestion based on shortcut path/name
        /// Enhanced with Phosphor icon suggestions
        /// </summary>
        public static string SuggestIcon(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(label))
                return GetDefaultIcon();

            var searchText = $"{path} {label}".ToLowerInvariant();

            // Application-specific suggestions
            if (searchText.Contains("calc") || searchText.Contains("calculator"))
                return "calculator";
            else if (searchText.Contains("explorer") || searchText.Contains("folder"))
                return "folder-open";
            else if (searchText.Contains("paint") || searchText.Contains("mspaint"))
                return "palette";
            else if (searchText.Contains("cmd") || searchText.Contains("command") || searchText.Contains("terminal") || searchText.Contains("powershell"))
                return "terminal-window";

            // Development tools
            else if (searchText.Contains("github") || searchText.Contains("git"))
                return "github-logo";
            else if (searchText.Contains("code") || searchText.Contains("visual studio") || searchText.Contains("vscode"))
                return "brackets-curly";
            else if (searchText.Contains("database") || searchText.Contains("dbeaver") || searchText.Contains("sql"))
                return "database";
            else if (searchText.Contains("notepad") || searchText.Contains("text") || searchText.Contains("editor"))
                return "note-pencil";
            else if (searchText.Contains("filezilla") || searchText.Contains("ftp") || searchText.Contains("upload"))
                return "file-arrow-up";

            // Communication
            else if (searchText.Contains("discord"))
                return "discord-logo";
            else if (searchText.Contains("slack"))
                return "slack-logo";
            else if (searchText.Contains("teams"))
                return "microsoft-teams-logo";
            else if (searchText.Contains("messenger"))
                return "messenger-logo";
            else if (searchText.Contains("mail") || searchText.Contains("email") || searchText.Contains("outlook"))
                return searchText.Contains("outlook") ? "microsoft-outlook-logo" : "envelope";

            // Office applications
            else if (searchText.Contains("excel"))
                return "microsoft-excel-logo";
            else if (searchText.Contains("word"))
                return "microsoft-word-logo";

            // Web and URLs
            else if (searchText.Contains("http") || searchText.Contains("www") || searchText.Contains("browser"))
                return "link-simple";

            // Gaming
            else if (searchText.Contains("steam"))
                return "steam-logo";
            else if (searchText.Contains("game") || searchText.Contains("gaming"))
                return "joystick";
            else if (searchText.Contains("vlc") || searchText.Contains("media"))
                return "traffic-cone";

            // Design and creative
            else if (searchText.Contains("photoshop") || searchText.Contains("gimp") || searchText.Contains("image"))
                return "image";
            else if (searchText.Contains("krita") || searchText.Contains("art") || searchText.Contains("draw"))
                return "palette";

            // System tools
            else if (searchText.Contains("archive") || searchText.Contains("zip") || searchText.Contains("7-zip"))
                return "archive";
            else if (searchText.Contains("monitor") || searchText.Contains("system") || searchText.Contains("task"))
                return "pulse";

            // Finance and crypto
            else if (searchText.Contains("bitcoin") || searchText.Contains("btc") || searchText.Contains("crypto"))
                return "currency-btc";
            else if (searchText.Contains("money") || searchText.Contains("bank") || searchText.Contains("finance"))
                return "money";

            return GetDefaultIcon();
        }

        /// <summary>
        /// Gets icons by category for organized display
        /// </summary>
        public static Dictionary<string, List<string>> GetIconsByCategory()
        {
            return new Dictionary<string, List<string>>
            {
                ["Default"] = new List<string> { "folder-open", "calculator", "palette", "terminal-window" },
                ["Development"] = new List<string> { "database", "github-logo", "file-arrow-up", "note-pencil", "brackets-curly", "dev-to-logo", "hash", "package", "stack", "open-ai-logo" },
                ["Communication"] = new List<string> { "envelope", "chat-text", "discord-logo", "slack-logo", "microsoft-teams-logo", "messenger-logo" },
                ["Office"] = new List<string> { "microsoft-excel-logo", "microsoft-outlook-logo", "microsoft-word-logo" },
                ["System"] = new List<string> { "app-window", "archive", "desktop", "wrench", "pulse" },
                ["Creative"] = new List<string> { "bookmarks", "checkerboard", "image", "puzzle-piece" },
                ["Network"] = new List<string> { "link-simple", "globe", "cell-signal-full", "cloud", "export" },
                ["Finance"] = new List<string> { "currency-btc", "currency-dollar", "money", "chart-line" },
                ["Entertainment"] = new List<string> { "joystick", "steam-logo", "traffic-cone" },
                ["Miscellaneous"] = new List<string> { "planet", "potted-plant", "question", "rocket-launch", "confetti", "user" }
            };
        }

        /// <summary>
        /// Gets the total number of available icons
        /// </summary>
        public static int GetIconCount()
        {
            return BuiltInIcons.Count;
        }
    }
}