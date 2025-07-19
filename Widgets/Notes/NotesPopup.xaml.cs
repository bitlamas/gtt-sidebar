using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace gtt_sidebar.Widgets.Notes
{
    public partial class NotesPopup : Window
    {
        private NotesData _notesData;
        private DispatcherTimer _autoSaveTimer;
        private bool _contentChanged = false;
        private int _maxTabs = 8;
        private bool _isInitializing = false;
        private string _currentlyEditingTabId; // Track which tab we're actually editing

        public event Action<NotesData> NotesChanged;

        public NotesPopup(NotesData notesData)
        {
            InitializeComponent();
            UpdateNotesData(notesData);
            InitializeAutoSave();
            RestoreWindowPosition();

            // Debug: Validate data integrity on startup
            ValidateTabDataIntegrity();
        }

        /// <summary>
        /// Debug method to validate tab data integrity
        /// </summary>
        private void ValidateTabDataIntegrity()
        {
            if (_notesData?.Tabs == null)
            {
                System.Diagnostics.Debug.WriteLine("ValidateTabDataIntegrity: No tabs data");
                return;
            }

            var defaultTabs = _notesData.Tabs.Where(t => t.IsDefault).ToList();
            var tabsWithoutIds = _notesData.Tabs.Where(t => string.IsNullOrEmpty(t.Id)).ToList();
            var duplicateIds = _notesData.Tabs.GroupBy(t => t.Id).Where(g => g.Count() > 1).ToList();

            System.Diagnostics.Debug.WriteLine($"ValidateTabDataIntegrity: {_notesData.Tabs.Count} total tabs, {defaultTabs.Count} default tabs, {tabsWithoutIds.Count} without IDs, {duplicateIds.Count()} duplicate ID groups");

            // Show content summary for each tab
            for (int i = 0; i < _notesData.Tabs.Count; i++)
            {
                var tab = _notesData.Tabs[i];
                var contentPreview = (tab.Content ?? "").Length > 20 ? (tab.Content ?? "").Substring(0, 20) + "..." : (tab.Content ?? "");
                System.Diagnostics.Debug.WriteLine($"  Tab[{i}]: ID={tab.Id}, Default={tab.IsDefault}, Content='{contentPreview}'");
            }

            if (defaultTabs.Count != 1)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateTabDataIntegrity: WARNING - Should have exactly 1 default tab, found {defaultTabs.Count}");
            }

            if (tabsWithoutIds.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateTabDataIntegrity: WARNING - {tabsWithoutIds.Count} tabs without IDs");
            }

            if (duplicateIds.Count() > 0)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateTabDataIntegrity: WARNING - Found duplicate IDs");
                foreach (var group in duplicateIds)
                {
                    System.Diagnostics.Debug.WriteLine($"  Duplicate ID: {group.Key} ({group.Count()} tabs)");
                }
            }
        }

        /// <summary>
        /// Updates the popup with new notes data (for reuse)
        /// </summary>
        public void UpdateNotesData(NotesData notesData)
        {
            _isInitializing = true;

            // Ensure we have valid data
            if (notesData == null)
            {
                System.Diagnostics.Debug.WriteLine("NotesPopup: Received null notesData, creating default");
                notesData = NotesStorage.LoadNotes(); // This should never return null
            }

            _notesData = notesData;

            // Additional safety check
            if (_notesData?.Tabs == null)
            {
                System.Diagnostics.Debug.WriteLine("NotesPopup: _notesData.Tabs is null, ensuring default tab");
                _notesData = NotesStorage.LoadNotes();
            }

            LoadTabs();
            SetActiveTab(_notesData.LastActiveTabIndex);

            _isInitializing = false;
        }

        private void InitializeAutoSave()
        {
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(15);
            _autoSaveTimer.Tick += (s, e) => SaveIfChanged();
            _autoSaveTimer.Start();
        }

        private void LoadTabs()
        {
            TabsContainer.Children.Clear();

            // Safety check
            if (_notesData?.Tabs == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadTabs: _notesData or Tabs is null");
                AddTabButton.IsEnabled = false;
                return;
            }

            System.Diagnostics.Debug.WriteLine($"LoadTabs: Creating UI for {_notesData.Tabs.Count} tabs:");

            for (int i = 0; i < _notesData.Tabs.Count; i++)
            {
                var tab = _notesData.Tabs[i];
                System.Diagnostics.Debug.WriteLine($"  [{i}] ID: {tab.Id}, IsDefault: {tab.IsDefault}, Title: '{tab.Title}', HasCloseButton: {!tab.IsDefault}");

                var tabButton = CreateTabButton(tab, i);
                TabsContainer.Children.Add(tabButton);
            }

            // Update add button state
            AddTabButton.IsEnabled = _notesData.Tabs.Count < _maxTabs;

            System.Diagnostics.Debug.WriteLine($"LoadTabs: Complete. Active tab index: {_notesData.LastActiveTabIndex}");
        }

        private Button CreateTabButton(NoteTab tab, int index)
        {
            var tabButton = new Button
            {
                Height = 22,
                MinWidth = 30,
                Margin = new Thickness(1, 0, 1, 0),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                FontSize = 9,
                Tag = index // Store index for tab switching
            };

            // Create content with title and close button
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var titleText = new TextBlock
            {
                Text = string.IsNullOrEmpty(tab.Title) ? (tab.IsDefault ? "📄" : "📝") : tab.Title,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            Grid.SetColumn(titleText, 0);
            grid.Children.Add(titleText);

            // Add close button for non-default tabs
            if (!tab.IsDefault)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var closeButton = new Button
                {
                    Content = "×",
                    Width = 14,
                    Height = 14,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    Margin = new Thickness(0, 0, 2, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = new { TabId = tab.Id, TabIndex = index } // Store both ID and index for debugging
                };

                closeButton.Click += CloseTabButton_Click;
                closeButton.MouseEnter += (s, e) => closeButton.Foreground = Brushes.Red;
                closeButton.MouseLeave += (s, e) => closeButton.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));

                Grid.SetColumn(closeButton, 1);
                grid.Children.Add(closeButton);
            }

            tabButton.Content = grid;
            tabButton.Click += (s, e) => SetActiveTab(index);

            return tabButton;
        }

        private void SetActiveTab(int index)
        {
            // Safety check
            if (_notesData?.Tabs == null || _notesData.Tabs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("SetActiveTab: No tabs available");
                return;
            }

            // Save current tab content first (using the tab ID we were editing, not the index)
            if (!_isInitializing && !string.IsNullOrEmpty(_currentlyEditingTabId))
            {
                SaveCurrentTabContentById(_currentlyEditingTabId);
            }

            // Validate index
            if (index < 0 || index >= _notesData.Tabs.Count)
            {
                index = 0; // Default to first tab
            }

            _notesData.LastActiveTabIndex = index;
            var activeTab = _notesData.Tabs[index];

            // Track which tab we're now editing
            _currentlyEditingTabId = activeTab.Id;

            System.Diagnostics.Debug.WriteLine($"SetActiveTab: Switching to tab {index} (ID: {activeTab.Id}, Title: '{activeTab.Title}')");
            System.Diagnostics.Debug.WriteLine($"SetActiveTab: Loading content: '{(activeTab.Content ?? "").Substring(0, Math.Min(30, (activeTab.Content ?? "").Length))}...'");

            // Update content - simple text assignment
            ContentTextBox.Text = activeTab.Content ?? "";

            if (!_isInitializing)
            {
                _contentChanged = false;
            }

            // Update tab visual states
            UpdateTabVisualStates();

            // Focus content area
            if (!_isInitializing)
            {
                ContentTextBox.Focus();
            }
        }

        // Removed markdown processing methods - using plain text for now

        private void UpdateTabVisualStates()
        {
            for (int i = 0; i < TabsContainer.Children.Count; i++)
            {
                var tabButton = (Button)TabsContainer.Children[i];
                if (i == _notesData.LastActiveTabIndex)
                {
                    tabButton.Background = new SolidColorBrush(Color.FromRgb(230, 240, 255));
                    tabButton.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
                }
                else
                {
                    tabButton.Background = new SolidColorBrush(Colors.White);
                    tabButton.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                }
            }
        }

        private void SaveCurrentTabContent()
        {
            if (!string.IsNullOrEmpty(_currentlyEditingTabId))
            {
                SaveCurrentTabContentById(_currentlyEditingTabId);
            }
        }

        private void SaveCurrentTabContentById(string tabId)
        {
            if (string.IsNullOrEmpty(tabId))
            {
                System.Diagnostics.Debug.WriteLine("SaveCurrentTabContentById: No tab ID provided");
                return;
            }

            var tab = _notesData.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCurrentTabContentById: Tab {tabId} not found");
                return;
            }

            var currentContent = ContentTextBox.Text ?? "";
            System.Diagnostics.Debug.WriteLine($"SaveCurrentTabContentById: Saving content to tab {tabId} ('{tab.Title}'): '{currentContent.Substring(0, Math.Min(20, currentContent.Length))}...'");

            tab.Content = currentContent;
            _contentChanged = true;
        }

        private void SaveIfChanged()
        {
            if (_contentChanged && !_isInitializing)
            {
                SaveCurrentTabContent(); // This now uses ID-based saving
                SaveWindowPosition();
                NotesChanged?.Invoke(_notesData);
                _contentChanged = false;
                System.Diagnostics.Debug.WriteLine("Auto-saved notes");
            }
        }

        private void SaveWindowPosition()
        {
            _notesData.PopupPosition.Left = this.Left;
            _notesData.PopupPosition.Top = this.Top;
        }

        private void RestoreWindowPosition()
        {
            if (_notesData.PopupPosition.IsValid)
            {
                // Ensure the window is still on screen
                if (_notesData.PopupPosition.Left >= 0 &&
                    _notesData.PopupPosition.Top >= 0 &&
                    _notesData.PopupPosition.Left + this.Width <= SystemParameters.PrimaryScreenWidth &&
                    _notesData.PopupPosition.Top + this.Height <= SystemParameters.PrimaryScreenHeight)
                {
                    this.Left = _notesData.PopupPosition.Left;
                    this.Top = _notesData.PopupPosition.Top;
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
        }

        private void AddTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Save current content before creating new tab
            SaveCurrentTabContent();

            var newTab = _notesData.AddNewTab(_maxTabs);
            if (newTab != null)
            {
                System.Diagnostics.Debug.WriteLine($"AddTabButton_Click: Created new tab with ID {newTab.Id}");
                LoadTabs();
                SetActiveTab(_notesData.Tabs.Count - 1); // Switch to new tab
                NotesChanged?.Invoke(_notesData);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AddTabButton_Click: Failed to create new tab (max {_maxTabs} reached)");
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tagData = button.Tag;

            string tabId = null;
            int tabIndex = -1;

            // Handle both old and new tag formats
            if (tagData is string)
            {
                tabId = (string)tagData;
            }
            else if (tagData != null)
            {
                // Anonymous type with TabId and TabIndex
                var tabInfo = tagData.GetType();
                var tabIdProperty = tabInfo.GetProperty("TabId");
                var tabIndexProperty = tabInfo.GetProperty("TabIndex");

                if (tabIdProperty != null && tabIndexProperty != null)
                {
                    tabId = tabIdProperty.GetValue(tagData) as string;
                    tabIndex = (int)(tabIndexProperty.GetValue(tagData) ?? -1);
                }
            }

            if (string.IsNullOrEmpty(tabId))
            {
                System.Diagnostics.Debug.WriteLine("CloseTabButton_Click: No tab ID found");
                return;
            }

            // Find the tab to delete
            var tabToDelete = _notesData.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tabToDelete == null)
            {
                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Tab with ID {tabId} not found");
                return;
            }

            if (tabToDelete.IsDefault)
            {
                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Attempted to delete default tab {tabId} - BLOCKED");
                return;
            }

            var tabIndexToDelete = _notesData.Tabs.IndexOf(tabToDelete);
            System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Deleting tab '{tabToDelete.Title}' (ID: {tabId}, Index: {tabIndexToDelete}, IsDefault: {tabToDelete.IsDefault})");

            // CRITICAL: Only save content if we're currently editing the tab we're about to delete
            if (_currentlyEditingTabId == tabId)
            {
                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Saving content for tab being deleted ({tabId})");
                SaveCurrentTabContentById(tabId);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: NOT saving content - deleting {tabId} but editing {_currentlyEditingTabId}");
            }

            // Remove the tab
            if (_notesData.RemoveTab(tabId))
            {
                // Always switch to default tab after deletion
                var defaultTab = _notesData.Tabs.FirstOrDefault(t => t.IsDefault);
                if (defaultTab != null)
                {
                    _notesData.LastActiveTabIndex = _notesData.Tabs.IndexOf(defaultTab);
                    System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Switching to default tab (index {_notesData.LastActiveTabIndex})");
                }
                else
                {
                    _notesData.LastActiveTabIndex = 0; // Fallback to first tab
                }

                // Reload the tab UI
                LoadTabs();
                SetActiveTab(_notesData.LastActiveTabIndex);
                NotesChanged?.Invoke(_notesData);

                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Successfully deleted tab. Active tab now: {_notesData.LastActiveTabIndex} (ID: {_currentlyEditingTabId})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CloseTabButton_Click: Failed to remove tab {tabId}");
            }
        }

        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _contentChanged = true;

            // Update tab title if this is the active tab
            if (!string.IsNullOrEmpty(_currentlyEditingTabId))
            {
                var activeTab = _notesData.Tabs.FirstOrDefault(t => t.Id == _currentlyEditingTabId);
                if (activeTab != null)
                {
                    var oldTitle = activeTab.Title;

                    // Temporarily update content to get new title
                    activeTab.Content = ContentTextBox.Text ?? "";
                    var newTitle = activeTab.Title;

                    if (oldTitle != newTitle)
                    {
                        // Find the tab button for this tab ID and update its text
                        var tabIndex = _notesData.Tabs.IndexOf(activeTab);
                        if (tabIndex >= 0 && tabIndex < TabsContainer.Children.Count)
                        {
                            var tabButton = (Button)TabsContainer.Children[tabIndex];
                            var grid = (Grid)tabButton.Content;
                            var titleText = (TextBlock)grid.Children[0];
                            titleText.Text = string.IsNullOrEmpty(newTitle) ? (activeTab.IsDefault ? "📄" : "📝") : newTitle;
                        }
                    }
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC to close
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine($"Window_KeyDown (ESC): Saving content for tab {_currentlyEditingTabId}");
                SaveIfChanged();
                this.Hide();
                return;
            }

            // Ctrl+Tab to cycle through tabs
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                var nextIndex = (_notesData.LastActiveTabIndex + 1) % _notesData.Tabs.Count;
                System.Diagnostics.Debug.WriteLine($"Window_KeyDown (Ctrl+Tab): Switching from tab {_notesData.LastActiveTabIndex} to {nextIndex}");
                SetActiveTab(nextIndex);
            }
        }

        // Simplified dragging - only from the drag handle
        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                    SaveWindowPosition();
                    NotesChanged?.Invoke(_notesData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Drag error: {ex.Message}");
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Save current tab content before closing
            System.Diagnostics.Debug.WriteLine($"Window_Deactivated: Saving content for tab {_currentlyEditingTabId}");
            SaveIfChanged();
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Save before closing
            SaveIfChanged();

            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Tick -= (sender, args) => SaveIfChanged();
                _autoSaveTimer = null;
            }

            base.OnClosing(e);
        }

        public void PositionRelativeToSidebar(Window sidebar)
        {
            // Only position relative to sidebar if no saved position exists
            if (!_notesData.PopupPosition.IsValid)
            {
                // Position 5px to the left of the sidebar
                this.Left = sidebar.Left - this.Width - 5;
                this.Top = sidebar.Top + 50; // Offset from top to align with notes widget

                // Ensure popup stays on screen
                if (this.Left < 0)
                {
                    this.Left = sidebar.Left + sidebar.Width + 5; // Show on right if no room on left
                }
            }
        }
    }
}