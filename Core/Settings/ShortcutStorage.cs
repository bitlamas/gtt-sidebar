using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Handles loading and saving shortcuts data to JSON file with async operations
    /// </summary>
    public static class ShortcutsStorage
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _shortcutsFilePath = Path.Combine(_appDataPath, "shortcuts.json");
        private static readonly string _iconsPath = Path.Combine(_appDataPath, "icons");

        /// <summary>
        /// Async version - loads shortcuts data from file, creates default if file doesn't exist
        /// </summary>
        public static async Task<ShortcutsData> LoadShortcutsAsync()
        {
            try
            {
                // ensure directories exist
                Directory.CreateDirectory(_appDataPath);
                Directory.CreateDirectory(_iconsPath);

                if (!File.Exists(_shortcutsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Shortcuts file doesn't exist, creating default");
                    var newData = ShortcutsData.CreateDefault();
                    await SaveShortcutsAsync(newData);
                    return newData;
                }

                var json = await File.ReadAllTextAsync(_shortcutsFilePath);
                var shortcutsData = JsonConvert.DeserializeObject<ShortcutsData>(json);

                // validate data integrity
                if (shortcutsData == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid shortcuts data, creating default");
                    var newData = ShortcutsData.CreateDefault();
                    await SaveShortcutsAsync(newData);
                    return newData;
                }

                // clean up any invalid data
                shortcutsData.ValidateAndCleanup();

                if (shortcutsData.Shortcuts.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No shortcuts found - empty state");
                    // don't auto-create defaults anymore
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {shortcutsData.Shortcuts.Count} shortcuts");
                return shortcutsData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading shortcuts: {ex.Message}");
                var newData = ShortcutsData.CreateDefault();
                await SaveShortcutsAsync(newData);
                return newData;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static ShortcutsData LoadShortcuts()
        {
            return Task.Run(async () => await LoadShortcutsAsync()).Result;
        }

        /// <summary>
        /// Async version - saves shortcuts data to file
        /// </summary>
        public static async Task<bool> SaveShortcutsAsync(ShortcutsData shortcutsData)
        {
            try
            {
                // ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                // validate data before saving
                if (shortcutsData?.Shortcuts == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save null shortcuts data");
                    return false;
                }

                // clean up data before saving
                shortcutsData.ValidateAndCleanup();

                var json = JsonConvert.SerializeObject(shortcutsData, Formatting.Indented);
                await File.WriteAllTextAsync(_shortcutsFilePath, json);

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
        /// Synchronous version for backward compatibility
        /// </summary>
        public static bool SaveShortcuts(ShortcutsData shortcutsData)
        {
            return Task.Run(async () => await SaveShortcutsAsync(shortcutsData)).Result;
        }

        /// <summary>
        /// Saves a custom icon from a bitmap encoder and returns the filename
        /// </summary>
        public static async Task<string> SaveCustomIconFromBitmapAsync(PngBitmapEncoder encoder, string fileName)
        {
            try
            {
                // ensure icons directory exists
                Directory.CreateDirectory(_iconsPath);

                var destinationPath = Path.Combine(_iconsPath, fileName);

                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    encoder.Save(fileStream);
                    await fileStream.FlushAsync();
                }

                System.Diagnostics.Debug.WriteLine($"SaveCustomIconFromBitmapAsync: Saved {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom icon from bitmap: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static string SaveCustomIconFromBitmap(PngBitmapEncoder encoder, string fileName)
        {
            return Task.Run(async () => await SaveCustomIconFromBitmapAsync(encoder, fileName)).Result;
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
        /// Async version - saves a custom icon file and returns the relative path for storage
        /// </summary>
        public static async Task<string> SaveCustomIconAsync(string sourceFilePath, string shortcutId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("SaveCustomIconAsync: Source file doesn't exist");
                    return null;
                }

                // ensure icons directory exists
                Directory.CreateDirectory(_iconsPath);

                // generate unique filename
                var extension = Path.GetExtension(sourceFilePath);
                var fileName = $"{shortcutId}{extension}";
                var destinationPath = Path.Combine(_iconsPath, fileName);

                // copy file to icons directory
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                System.Diagnostics.Debug.WriteLine($"SaveCustomIconAsync: Saved icon for {shortcutId} to {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom icon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static string SaveCustomIcon(string sourceFilePath, string shortcutId)
        {
            return Task.Run(async () => await SaveCustomIconAsync(sourceFilePath, shortcutId)).Result;
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
        public static async Task<bool> BackupShortcutsAsync()
        {
            try
            {
                if (!File.Exists(_shortcutsFilePath))
                    return false;

                var backupPath = Path.Combine(_appDataPath, $"shortcuts_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                using (var sourceStream = new FileStream(_shortcutsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

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
        public static async Task<bool> ResetToDefaultAsync()
        {
            try
            {
                if (File.Exists(_shortcutsFilePath))
                {
                    await BackupShortcutsAsync(); // backup first
                    File.Delete(_shortcutsFilePath);
                }

                var defaultData = ShortcutsData.CreateDefault();
                return await SaveShortcutsAsync(defaultData);
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

            // check that all shortcuts have valid data
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

                // collect all custom icons currently in use
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

                // delete orphaned icon files
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