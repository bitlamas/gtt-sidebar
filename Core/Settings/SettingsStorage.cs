using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Handles loading and saving settings data to JSON file with async operations
    /// </summary>
    public static class SettingsStorage
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _settingsFilePath = Path.Combine(_appDataPath, "settings.json");

        /// <summary>
        /// Async version - loads settings data from file, creates default if file doesn't exist
        /// </summary>
        public static async Task<SettingsData> LoadSettingsAsync()
        {
            try
            {
                // ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                if (!File.Exists(_settingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Settings file doesn't exist, creating default");
                    var newSettings = CreateDefaultSettings();
                    await SaveSettingsAsync(newSettings);
                    return newSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonConvert.DeserializeObject<SettingsData>(json);

                // validate data integrity
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid settings data, creating default");
                    var newSettings = CreateDefaultSettings();
                    await SaveSettingsAsync(newSettings);
                    return newSettings;
                }

                // validate and fix any out-of-range values
                if (settings.Window == null)
                {
                    settings.Window = new WindowSettings();
                }

                settings.Window.Validate();

                // check if settings would cause window to be off-screen
                if (!settings.Window.FitsOnScreen())
                {
                    System.Diagnostics.Debug.WriteLine("Settings would place window off-screen, using defaults");
                    settings.Window = new WindowSettings();
                }

                System.Diagnostics.Debug.WriteLine("Loaded settings successfully");
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                var newSettings = CreateDefaultSettings();
                await SaveSettingsAsync(newSettings);
                return newSettings;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static SettingsData LoadSettings()
        {
            // run async version synchronously for compatibility
            return Task.Run(async () => await LoadSettingsAsync()).Result;
        }

        /// <summary>
        /// Async version - saves settings data to file
        /// </summary>
        public static async Task<bool> SaveSettingsAsync(SettingsData settings)
        {
            try
            {
                // ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                // validate data before saving
                if (settings?.Window == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save null settings data");
                    return false;
                }

                // validate settings before saving
                settings.Window.Validate();

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(_settingsFilePath, json);

                System.Diagnostics.Debug.WriteLine("Settings saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static bool SaveSettings(SettingsData settings)
        {
            // run async version synchronously for compatibility
            return Task.Run(async () => await SaveSettingsAsync(settings)).Result;
        }

        /// <summary>
        /// Creates fresh default settings data
        /// </summary>
        private static SettingsData CreateDefaultSettings()
        {
            var settings = new SettingsData
            {
                Window = new WindowSettings
                {
                    Position = SidebarPosition.Right,
                    Width = 122,
                    MarginTop = 5,
                    MarginBottom = 5,
                    MarginSide = 5
                }
            };

            System.Diagnostics.Debug.WriteLine("Created default settings");
            return settings;
        }

        /// <summary>
        /// Gets the path where settings are stored (for debugging)
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }

        /// <summary>
        /// Backs up the current settings file
        /// </summary>
        public static async Task<bool> BackupSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                    return false;

                var backupPath = Path.Combine(_appDataPath, $"settings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                await File.CopyAsync(_settingsFilePath, backupPath);

                System.Diagnostics.Debug.WriteLine($"Settings backed up to: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error backing up settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets settings to default state
        /// </summary>
        public static async Task<bool> ResetToDefaultAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    await BackupSettingsAsync(); // backup first
                    File.Delete(_settingsFilePath);
                }

                var defaultSettings = CreateDefaultSettings();
                return await SaveSettingsAsync(defaultSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates that proposed settings are safe to apply
        /// </summary>
        public static bool ValidateSettings(SettingsData settings)
        {
            if (settings?.Window == null)
                return false;

            // check basic validation
            if (!settings.Window.IsValid())
                return false;

            // check screen fit
            if (!settings.Window.FitsOnScreen())
                return false;

            return true;
        }
    }

    // extension method for File.CopyAsync (not available in .NET Framework 4.7.2)
    public static class FileExtensions
    {
        public static async Task CopyAsync(string source, string destination)
        {
            using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            using (var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }
}