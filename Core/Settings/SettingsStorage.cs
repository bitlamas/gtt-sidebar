using System;
using System.IO;
using Newtonsoft.Json;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Handles loading and saving settings data to JSON file
    /// </summary>
    public static class SettingsStorage
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _settingsFilePath = Path.Combine(_appDataPath, "settings.json");

        /// <summary>
        /// Loads settings data from file, creates default if file doesn't exist
        /// </summary>
        public static SettingsData LoadSettings()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                if (!File.Exists(_settingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Settings file doesn't exist, creating default");
                    var newSettings = CreateDefaultSettings();
                    SaveSettings(newSettings); // Save immediately
                    return newSettings;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonConvert.DeserializeObject<SettingsData>(json);

                // Validate data integrity
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid settings data, creating default");
                    var newSettings = CreateDefaultSettings();
                    SaveSettings(newSettings);
                    return newSettings;
                }

                // Validate and fix any out-of-range values
                if (settings.Window == null)
                {
                    settings.Window = new WindowSettings();
                }

                settings.Window.Validate();

                // Check if settings would cause window to be off-screen
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
                SaveSettings(newSettings);
                return newSettings;
            }
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
        /// Saves settings data to file
        /// </summary>
        public static bool SaveSettings(SettingsData settings)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                // Validate data before saving
                if (settings?.Window == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save null settings data");
                    return false;
                }

                // Validate settings before saving
                settings.Window.Validate();

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);

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
        /// Gets the path where settings are stored (for debugging)
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }

        /// <summary>
        /// Backs up the current settings file
        /// </summary>
        public static bool BackupSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                    return false;

                var backupPath = Path.Combine(_appDataPath, $"settings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(_settingsFilePath, backupPath);

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
        public static bool ResetToDefault()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    BackupSettings(); // Backup first
                    File.Delete(_settingsFilePath);
                }

                var defaultSettings = CreateDefaultSettings();
                return SaveSettings(defaultSettings);
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

            // Check basic validation
            if (!settings.Window.IsValid())
                return false;

            // Check screen fit
            if (!settings.Window.FitsOnScreen())
                return false;

            return true;
        }
    }
}