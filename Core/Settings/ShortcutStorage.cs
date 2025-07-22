using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Handles loading and saving shortcuts data to JSON file
    /// </summary>
    public static class ShortcutsStorage
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _shortcutsFilePath = Path.Combine(_appDataPath, "shortcuts.json");
        private static readonly string _iconsPath = Path.Combine(_appDataPath, "icons");

        /// <summary>
        /// Loads shortcuts data from file, creates default if file doesn't exist
        /// </summary>
        public static ShortcutsData LoadShortcuts()
        {
            try
            {
                // Ensure directories exist
                Directory.CreateDirectory(_appDataPath);
                Directory.CreateDirectory(_iconsPath);

                if (!File.Exists(_shortcutsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Shortcuts file doesn't exist, creating default");
                    var newData = ShortcutsData.CreateDefault();
                    SaveShortcuts(newData); // Save immediately
                    return newData;
                }

                var json = File.ReadAllText(_shortcutsFilePath);
                var shortcutsData = JsonConvert.DeserializeObject<ShortcutsData>(json);

                // Validate data integrity
                if (shortcutsData == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid shortcuts data, creating default");
                    var newData = ShortcutsData.CreateDefault();
                    SaveShortcuts(newData);
                    return newData;
                }

                // Clean up any invalid data
                shortcutsData.ValidateAndCleanup();

                if (shortcutsData.Shortcuts.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No shortcuts found - empty state");
                    // Don't auto-create defaults anymore
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {shortcutsData.Shortcuts.Count} shortcuts");
                return shortcutsData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading shortcuts: {ex.Message}");
                var newData = ShortcutsData.CreateDefault();
                SaveShortcuts(newData);
                return newData;
            }
        }
        /// <summary>
        /// Saves a custom icon from a bitmap encoder and returns the filename
        /// </summary>
        public static string SaveCustomIconFromBitmap(PngBitmapEncoder encoder, string fileName)
        {
            try
            {
                // Ensure icons directory exists
                Directory.CreateDirectory(_iconsPath);

                var destinationPath = Path.Combine(_iconsPath, fileName);

                using (var fileStream = new FileStream(destinationPath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                System.Diagnostics.Debug.WriteLine($"SaveCustomIconFromBitmap: Saved {fileName}");
                return fileName; // Return just the filename, not full path
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom icon from bitmap: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Saves shortcuts data to file
        /// </summary>
        public static bool SaveShortcuts(ShortcutsData shortcutsData)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                // Validate data before saving
                if (shortcutsData?.Shortcuts == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save null shortcuts data");
                    return false;
                }

                // Clean up data before saving
                shortcutsData.ValidateAndCleanup();

                var json = JsonConvert.SerializeObject(shortcutsData, Formatting.Indented);
                File.WriteAllText(_shortcutsFilePath, json);

                System.Diagnostics.Debug.WriteLine($"Saved {shortcutsData.Shortcuts.Count} shortcuts");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving shortcuts: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the path where shortcuts are stored (for debugging)
        /// </summary>
        public static string GetShortcutsFilePath()
        {
            return _shortcutsFilePath;
        }

        /// <summary>
        /// Gets the path where custom icons are stored
        /// </summary>
        public static string GetIconsPath()
        {
            return _iconsPath;
        }

        /// <summary>
        /// Saves a custom icon file and returns the relative path for storage
        /// </summary>
        public static string SaveCustomIcon(string sourceFilePath, string shortcutId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("SaveCustomIcon: Source file doesn't exist");
                    return null;
                }

                // Ensure icons directory exists
                Directory.CreateDirectory(_iconsPath);

                // Generate unique filename
                var extension = Path.GetExtension(sourceFilePath);
                var fileName = $"{shortcutId}{extension}";
                var destinationPath = Path.Combine(_iconsPath, fileName);

                // Copy file to icons directory
                File.Copy(sourceFilePath, destinationPath, true);

                System.Diagnostics.Debug.WriteLine($"SaveCustomIcon: Saved icon for {shortcutId} to {fileName}");
                return fileName; // Return just the filename, not full path
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom icon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the full path to a custom icon file
        /// </summary>
        public static string GetCustomIconPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var fullPath = Path.Combine(_iconsPath, fileName);
            return File.Exists(fullPath) ? fullPath : null;
        }

        /// <summary>
        /// Deletes a custom icon file
        /// </summary>
        public static bool DeleteCustomIcon(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return false;

                var fullPath = Path.Combine(_iconsPath, fileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    System.Diagnostics.Debug.WriteLine($"DeleteCustomIcon: Deleted {fileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting custom icon: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Backs up the current shortcuts file
        /// </summary>
        public static bool BackupShortcuts()
        {
            try
            {
                if (!File.Exists(_shortcutsFilePath))
                    return false;

                var backupPath = Path.Combine(_appDataPath, $"shortcuts_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(_shortcutsFilePath, backupPath);

                System.Diagnostics.Debug.WriteLine($"Shortcuts backed up to: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error backing up shortcuts: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets shortcuts to default state
        /// </summary>
        public static bool ResetToDefault()
        {
            try
            {
                if (File.Exists(_shortcutsFilePath))
                {
                    BackupShortcuts(); // Backup first
                    File.Delete(_shortcutsFilePath);
                }

                var defaultData = ShortcutsData.CreateDefault();
                return SaveShortcuts(defaultData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting shortcuts: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates that proposed shortcuts data is safe to save
        /// </summary>
        public static bool ValidateShortcuts(ShortcutsData shortcutsData)
        {
            if (shortcutsData?.Shortcuts == null)
                return false;

            // Check that all shortcuts have valid data
            foreach (var shortcut in shortcutsData.Shortcuts)
            {
                if (shortcut == null || !shortcut.IsValid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Cleans up orphaned custom icon files (icons with no corresponding shortcuts)
        /// </summary>
        public static void CleanupOrphanedIcons(ShortcutsData shortcutsData)
        {
            try
            {
                if (!Directory.Exists(_iconsPath))
                    return;

                var iconFiles = Directory.GetFiles(_iconsPath, "*.*", SearchOption.TopDirectoryOnly);
                var usedIcons = new System.Collections.Generic.HashSet<string>();

                // Collect all custom icons currently in use
                if (shortcutsData?.Shortcuts != null)
                {
                    foreach (var shortcut in shortcutsData.Shortcuts)
                    {
                        if (shortcut.IconType == "custom" && !string.IsNullOrWhiteSpace(shortcut.IconValue))
                        {
                            usedIcons.Add(shortcut.IconValue);
                        }
                    }
                }

                // Delete orphaned icon files
                foreach (var iconFile in iconFiles)
                {
                    var fileName = Path.GetFileName(iconFile);
                    if (!usedIcons.Contains(fileName))
                    {
                        try
                        {
                            File.Delete(iconFile);
                            System.Diagnostics.Debug.WriteLine($"CleanupOrphanedIcons: Deleted orphaned icon {fileName}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete orphaned icon {fileName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during icon cleanup: {ex.Message}");
            }
        }
    }
}