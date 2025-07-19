using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using gtt_sidebar.Core.Interfaces;

namespace gtt_sidebar.Widgets.Notes
{
    public partial class NotesWidget : UserControl, IWidget
    {
        private NotesData _notesData;
        private NotesPopup _popup;

        public new string Name => "Notes";

        public NotesWidget()
        {
            InitializeComponent();

            // Make the entire widget clickable
            this.MouseLeftButtonDown += NotesWidget_MouseLeftButtonDown;
            this.Background = Brushes.Transparent; // Ensure whole area is clickable
        }

        public UserControl GetControl() => this;

        public async Task InitializeAsync()
        {
            try
            {
                // Load notes data from storage
                System.Diagnostics.Debug.WriteLine("NotesWidget: Loading notes from storage...");
                _notesData = NotesStorage.LoadNotes();

                if (_notesData == null)
                {
                    System.Diagnostics.Debug.WriteLine("NotesWidget: LoadNotes returned null!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NotesWidget: Loaded {_notesData.Tabs?.Count ?? 0} tabs");
                }

                UpdatePreview();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotesWidget InitializeAsync error: {ex.Message}");
                // Create emergency fallback
                _notesData = new NotesData();
                _notesData.EnsureDefaultTab("Hi. This is your notepad.");
            }
        }

        private void UpdatePreview()
        {
            try
            {
                if (_notesData?.Tabs?.Count > 0)
                {
                    // Show preview of the active tab
                    var activeTab = _notesData.GetActiveTab();
                    PreviewText.Text = activeTab?.GetPreview() ?? "Click to add notes...";

                    // Update tab count
                    var tabCount = _notesData.Tabs.Count;
                    TabCountText.Text = tabCount == 1 ? "1 tab" : $"{tabCount} tabs";
                }
                else
                {
                    PreviewText.Text = "Click to add notes...";
                    TabCountText.Text = "0 tabs";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreview error: {ex.Message}");
                PreviewText.Text = "Click to add notes...";
                TabCountText.Text = "0 tabs";
            }
        }

        private void NotesWidget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenNotesPopup();
        }

        private void OpenNotesPopup()
        {
            try
            {
                // Ensure we have valid notes data
                if (_notesData == null)
                {
                    System.Diagnostics.Debug.WriteLine("NotesWidget: _notesData is null in OpenNotesPopup, initializing...");
                    // This might happen if InitializeAsync wasn't called or failed
                    _notesData = NotesStorage.LoadNotes();
                    if (_notesData == null)
                    {
                        System.Diagnostics.Debug.WriteLine("NotesWidget: LoadNotes still returned null, creating emergency default");
                        _notesData = new NotesData();
                        _notesData.EnsureDefaultTab("Hi. This is your notepad.");
                    }
                    UpdatePreview();
                }

                // Debug: Log current state
                System.Diagnostics.Debug.WriteLine($"NotesWidget: Opening popup with {_notesData.Tabs.Count} tabs, active: {_notesData.LastActiveTabIndex}");

                // Get main window reference once
                var mainWindow = Application.Current.MainWindow;

                // If popup exists but is hidden, just show it
                if (_popup != null)
                {
                    // Update popup with current data (in case it was modified elsewhere)
                    _popup.UpdateNotesData(_notesData);

                    // Position and show
                    if (mainWindow != null)
                    {
                        _popup.PositionRelativeToSidebar(mainWindow);
                    }

                    _popup.Show();
                    _popup.Activate();
                    return;
                }

                // Create popup for the first time
                _popup = new NotesPopup(_notesData);
                _popup.NotesChanged += OnNotesChanged;

                // Handle popup closing to set it to null
                _popup.Closed += (s, e) =>
                {
                    if (_popup != null)
                    {
                        _popup.NotesChanged -= OnNotesChanged;
                        _popup = null;
                    }
                };

                // Position popup relative to sidebar
                if (mainWindow != null)
                {
                    _popup.PositionRelativeToSidebar(mainWindow);
                }

                // Show popup
                _popup.Show();
                _popup.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening notes popup: {ex.Message}");
                MessageBox.Show($"Error opening notes: {ex.Message}", "Notes Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnNotesChanged(NotesData updatedNotesData)
        {
            _notesData = updatedNotesData;
            UpdatePreview();

            // Save to storage
            NotesStorage.SaveNotes(_notesData);
        }

        public void Dispose()
        {
            try
            {
                // Save any pending changes
                if (_notesData != null)
                {
                    NotesStorage.SaveNotes(_notesData);
                }

                // Clean up popup properly
                if (_popup != null)
                {
                    _popup.NotesChanged -= OnNotesChanged;

                    // Force close popup if it's still open
                    if (_popup.IsVisible)
                    {
                        _popup.Hide();
                    }

                    _popup.Close();
                    _popup = null;
                }

                // Clear references
                _notesData = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing NotesWidget: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refresh the preview (useful if notes were modified externally)
        /// </summary>
        public void RefreshPreview()
        {
            try
            {
                _notesData = NotesStorage.LoadNotes();
                UpdatePreview();

                // If popup is open, update it too
                if (_popup != null && _popup.IsVisible)
                {
                    _popup.UpdateNotesData(_notesData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing notes preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets current notes data (for debugging or external access)
        /// </summary>
        public NotesData GetNotesData()
        {
            return _notesData;
        }

        /// <summary>
        /// Manually trigger save (useful for external calls)
        /// </summary>
        public bool SaveNotes()
        {
            try
            {
                if (_notesData != null)
                {
                    return NotesStorage.SaveNotes(_notesData);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error manually saving notes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Closes the popup if it's open (useful for external calls)
        /// </summary>
        public void ClosePopup()
        {
            try
            {
                if (_popup != null && _popup.IsVisible)
                {
                    _popup.Hide();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex.Message}");
            }
        }
    }
}