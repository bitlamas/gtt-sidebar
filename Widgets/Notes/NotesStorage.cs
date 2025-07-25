using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace gtt_sidebar.Widgets.Notes
{
    /// <summary>
    /// Handles loading and saving notes data to JSON file with async operations
    /// </summary>
    public static class NotesStorage
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _notesFilePath = Path.Combine(_appDataPath, "notes.json");
        private const string DEFAULT_CONTENT = "Hi. This is your notepad.";

        /// <summary>
        /// Async version - loads notes data from file, creates default if file doesn't exist
        /// </summary>
        public static async Task<NotesData> LoadNotesAsync()
        {
            try
            {
                // ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                if (!File.Exists(_notesFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Notes file doesn't exist, creating default");
                    var newData = CreateDefaultNotesData();
                    await SaveNotesAsync(newData);
                    return newData;
                }

                var json = await File.ReadAllTextAsync(_notesFilePath);
                var notesData = JsonConvert.DeserializeObject<NotesData>(json);

                // validate data integrity
                if (notesData == null || notesData.Tabs == null)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid notes data, creating default");
                    var newData = CreateDefaultNotesData();
                    await SaveNotesAsync(newData);
                    return newData;
                }

                // clean up and validate existing data
                CleanupNotesData(notesData);

                // ensure we always have exactly one default tab
                notesData.EnsureDefaultTab(DEFAULT_CONTENT);

                System.Diagnostics.Debug.WriteLine($"Loaded {notesData.Tabs.Count} notes tabs");
                return notesData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notes: {ex.Message}");
                var newData = CreateDefaultNotesData();
                await SaveNotesAsync(newData);
                return newData;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static NotesData LoadNotes()
        {
            return Task.Run(async () => await LoadNotesAsync()).Result;
        }

        /// <summary>
        /// Async version - saves notes data to file
        /// </summary>
        public static async Task<bool> SaveNotesAsync(NotesData notesData)
        {
            try
            {
                // ensure directory exists
                Directory.CreateDirectory(_appDataPath);

                // validate data before saving
                if (notesData?.Tabs == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot save null notes data");
                    return false;
                }

                var json = JsonConvert.SerializeObject(notesData, Formatting.Indented);
                await File.WriteAllTextAsync(_notesFilePath, json);

                System.Diagnostics.Debug.WriteLine($"Saved {notesData.Tabs.Count} notes tabs");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving notes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public static bool SaveNotes(NotesData notesData)
        {
            return Task.Run(async () => await SaveNotesAsync(notesData)).Result;
        }

        /// <summary>
        /// Creates fresh default notes data
        /// </summary>
        private static NotesData CreateDefaultNotesData()
        {
            var notesData = new NotesData();
            notesData.EnsureDefaultTab(DEFAULT_CONTENT);

            // debug: verify the default data is correct
            System.Diagnostics.Debug.WriteLine($"CreateDefaultNotesData: Created {notesData.Tabs.Count} tabs");
            var defaultTab = notesData.Tabs.FirstOrDefault(t => t.IsDefault);
            if (defaultTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDefaultNotesData: Default tab ID: {defaultTab.Id}, Content: '{defaultTab.Content}'");
            }

            return notesData;
        }

        /// <summary>
        /// Cleans up common data issues from older versions
        /// </summary>
        private static void CleanupNotesData(NotesData notesData)
        {
            if (notesData.Tabs == null)
            {
                notesData.Tabs = new System.Collections.Generic.List<NoteTab>();
                return;
            }

            // remove any tabs with invalid IDs
            var invalidTabs = notesData.Tabs.Where(t => string.IsNullOrEmpty(t.Id)).ToList();
            foreach (var invalid in invalidTabs)
            {
                notesData.Tabs.Remove(invalid);
                System.Diagnostics.Debug.WriteLine("Removed invalid tab without ID");
            }

            // fix tabs with duplicate IDs
            var duplicateGroups = notesData.Tabs.GroupBy(t => t.Id).Where(g => g.Count() > 1);
            foreach (var group in duplicateGroups)
            {
                var duplicates = group.Skip(1).ToList(); // keep first, remove rest
                foreach (var duplicate in duplicates)
                {
                    notesData.Tabs.Remove(duplicate);
                    System.Diagnostics.Debug.WriteLine($"Removed duplicate tab with ID: {duplicate.Id}");
                }
            }

            // remove sample markdown content if it exists
            foreach (var tab in notesData.Tabs)
            {
                if (tab.Content != null &&
                    (tab.Content.Contains("# Main Header") ||
                     tab.Content.Contains("## Markdown Formatting Help") ||
                     tab.Content.Contains("Try editing this text to see") ||
                     tab.Content.Contains("Markdown accepted"))) // remove old markdown reference
                {
                    tab.Content = tab.IsDefault ? DEFAULT_CONTENT : "";
                    System.Diagnostics.Debug.WriteLine("Cleared sample content from tab");
                }
            }

            // ensure valid active tab index
            if (notesData.LastActiveTabIndex < 0 || notesData.LastActiveTabIndex >= notesData.Tabs.Count)
            {
                notesData.LastActiveTabIndex = 0;
            }
        }

        /// <summary>
        /// Gets the path where notes are stored (for debugging)
        /// </summary>
        public static string GetNotesFilePath()
        {
            return _notesFilePath;
        }

        /// <summary>
        /// Backs up the current notes file
        /// </summary>
        public static async Task<bool> BackupNotesAsync()
        {
            try
            {
                if (!File.Exists(_notesFilePath))
                    return false;

                var backupPath = Path.Combine(_appDataPath, $"notes_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                using (var sourceStream = new FileStream(_notesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                System.Diagnostics.Debug.WriteLine($"Notes backed up to: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error backing up notes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets notes to default state (for debugging)
        /// </summary>
        public static async Task<bool> ResetToDefaultAsync()
        {
            try
            {
                if (File.Exists(_notesFilePath))
                {
                    await BackupNotesAsync(); // backup first
                    File.Delete(_notesFilePath);
                }

                var defaultData = CreateDefaultNotesData();
                return await SaveNotesAsync(defaultData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting notes: {ex.Message}");
                return false;
            }
        }
    }
}