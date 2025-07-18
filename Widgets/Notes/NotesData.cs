using System;
using System.Collections.Generic;
using System.Linq;

namespace gtt_sidebar.Widgets.Notes
{
    /// <summary>
    /// Main data structure for all notes
    /// </summary>
    public class NotesData
    {
        public List<NoteTab> Tabs { get; set; } = new List<NoteTab>();
        public int LastActiveTabIndex { get; set; } = 0;
        public WindowPosition PopupPosition { get; set; } = new WindowPosition();

        public NotesData()
        {
            // Do NOT automatically create tabs in constructor
            // Let the storage layer handle initial creation
        }

        /// <summary>
        /// Ensures there's exactly one default tab with proper content
        /// </summary>
        public void EnsureDefaultTab(string defaultContent = "")
        {
            // Remove any invalid default tabs first
            var invalidDefaults = Tabs.Where(t => t.IsDefault && string.IsNullOrEmpty(t.Id)).ToList();
            foreach (var invalid in invalidDefaults)
            {
                System.Diagnostics.Debug.WriteLine("EnsureDefaultTab: Removing invalid default tab without ID");
                Tabs.Remove(invalid);
            }

            // Check for existing default tab
            var defaultTab = Tabs.FirstOrDefault(t => t.IsDefault);

            if (defaultTab == null)
            {
                // Create the default tab if none exists
                var newDefault = new NoteTab
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = defaultContent,
                    IsDefault = true
                };
                Tabs.Insert(0, newDefault); // Always first
                LastActiveTabIndex = 0;
                System.Diagnostics.Debug.WriteLine($"EnsureDefaultTab: Created new default tab with ID {newDefault.Id}");
            }
            else
            {
                // Ensure existing default is valid
                if (string.IsNullOrEmpty(defaultTab.Id))
                {
                    defaultTab.Id = Guid.NewGuid().ToString();
                    System.Diagnostics.Debug.WriteLine($"EnsureDefaultTab: Assigned new ID {defaultTab.Id} to existing default tab");
                }
            }

            // Remove default flag from any other tabs (there should only be one default)
            var otherDefaults = Tabs.Where(t => t.IsDefault && t != Tabs.FirstOrDefault(x => x.IsDefault)).ToList();
            foreach (var other in otherDefaults)
            {
                other.IsDefault = false;
                System.Diagnostics.Debug.WriteLine($"EnsureDefaultTab: Removed default flag from tab {other.Id}");
            }

            // Debug: Log all tabs
            System.Diagnostics.Debug.WriteLine($"EnsureDefaultTab: Final state - {Tabs.Count} tabs:");
            for (int i = 0; i < Tabs.Count; i++)
            {
                var tab = Tabs[i];
                System.Diagnostics.Debug.WriteLine($"  [{i}] ID: {tab.Id}, IsDefault: {tab.IsDefault}, Title: '{tab.Title}'");
            }
        }

        /// <summary>
        /// Gets the currently active tab, ensuring validity
        /// </summary>
        public NoteTab GetActiveTab()
        {
            if (Tabs.Count == 0)
            {
                EnsureDefaultTab("Hi. This is your notepad. Markdown accepted.");
            }

            if (LastActiveTabIndex >= 0 && LastActiveTabIndex < Tabs.Count)
            {
                return Tabs[LastActiveTabIndex];
            }

            // Fallback to default tab
            var defaultTab = Tabs.FirstOrDefault(t => t.IsDefault);
            if (defaultTab != null)
            {
                LastActiveTabIndex = Tabs.IndexOf(defaultTab);
                return defaultTab;
            }

            // Fallback to first tab
            if (Tabs.Count > 0)
            {
                LastActiveTabIndex = 0;
                return Tabs[0];
            }

            // Emergency fallback - this should never happen
            EnsureDefaultTab("Hi. This is your notepad.");
            LastActiveTabIndex = 0;
            return Tabs[0];
        }

        /// <summary>
        /// Adds a new tab (up to maximum allowed)
        /// </summary>
        public NoteTab AddNewTab(int maxTabs = 8)
        {
            if (Tabs.Count >= maxTabs)
            {
                System.Diagnostics.Debug.WriteLine($"AddNewTab: Cannot add tab, already at max {maxTabs}");
                return null;
            }

            var newTab = new NoteTab
            {
                Id = Guid.NewGuid().ToString(),
                Content = "",
                IsDefault = false
            };

            Tabs.Add(newTab);
            System.Diagnostics.Debug.WriteLine($"AddNewTab: Created new tab with ID {newTab.Id}. Total tabs: {Tabs.Count}");
            return newTab;
        }

        /// <summary>
        /// Removes a tab (cannot remove default tab)
        /// </summary>
        public bool RemoveTab(string tabId)
        {
            if (string.IsNullOrEmpty(tabId))
            {
                System.Diagnostics.Debug.WriteLine("RemoveTab: Empty tab ID provided");
                return false;
            }

            var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveTab: Tab with ID {tabId} not found");
                return false;
            }

            if (tab.IsDefault)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveTab: Cannot delete default tab {tabId}");
                return false;
            }

            var index = Tabs.IndexOf(tab);
            System.Diagnostics.Debug.WriteLine($"RemoveTab: Removing tab '{tab.Title}' at index {index} (ID: {tabId})");

            Tabs.RemoveAt(index);

            // Adjust active tab index if needed
            if (LastActiveTabIndex >= index)
            {
                LastActiveTabIndex = Math.Max(0, LastActiveTabIndex - 1);
            }

            // Ensure we still have at least one tab (the default)
            if (Tabs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("RemoveTab: No tabs left! Re-creating default tab.");
                EnsureDefaultTab("Hi. This is your notepad.");
                LastActiveTabIndex = 0;
            }

            System.Diagnostics.Debug.WriteLine($"RemoveTab: Successfully removed tab. {Tabs.Count} tabs remaining. Active index: {LastActiveTabIndex}");
            return true;
        }
    }

    /// <summary>
    /// Window position for popup persistence
    /// </summary>
    public class WindowPosition
    {
        public double Left { get; set; } = double.NaN;
        public double Top { get; set; } = double.NaN;
        public bool IsValid => !double.IsNaN(Left) && !double.IsNaN(Top);
    }

    /// <summary>
    /// Individual note tab
    /// </summary>
    public class NoteTab
    {
        public string Id { get; set; }
        public string Content { get; set; } = "";
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Auto-generated title from first two non-whitespace characters
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return "";

                var nonWhitespaceChars = Content.Where(c => !char.IsWhiteSpace(c)).Take(2).ToArray();
                return new string(nonWhitespaceChars);
            }
        }

        /// <summary>
        /// Gets a preview of the content for the widget display
        /// </summary>
        public string GetPreview(int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return "Click to add notes...";

            // Simple text preview without markdown processing
            var preview = Content.Trim();

            if (preview.Length <= maxLength)
                return preview;

            return preview.Substring(0, maxLength) + "...";
        }
    }
}