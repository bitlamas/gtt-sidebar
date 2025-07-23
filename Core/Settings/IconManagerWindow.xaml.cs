using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace gtt_sidebar.Core.Settings
{
    public partial class IconManagerWindow : Window
    {
        private List<IconManagementItem> _allIcons = new List<IconManagementItem>();
        private List<IconManagementItem> _filteredIcons = new List<IconManagementItem>();

        public IconManagerWindow()
        {
            InitializeComponent();
            LoadIconsAsync();
        }

        private async void LoadIconsAsync()
        {
            try
            {
                await IconManager.InitializeAsync();
                _allIcons = IconManager.GetCatalogIcons();
                _filteredIcons = new List<IconManagementItem>(_allIcons);

                UpdateIconList();
                UpdateStatusDisplay();

                System.Diagnostics.Debug.WriteLine($"Loaded {_allIcons.Count} icons for management");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icons: {ex.Message}");
                StatusText.Text = "Error loading icon catalog";
            }
        }

        private void UpdateIconList()
        {
            IconListControl.ItemsSource = _filteredIcons;
        }

        private void UpdateStatusDisplay()
        {
            try
            {
                var selectedCount = _allIcons?.Count(i => i.IsSelected) ?? 0;
                var installedCount = _allIcons?.Count(i => i.IsDownloaded) ?? 0;
                var toDownloadCount = _allIcons?.Count(i => i.IsSelected && !i.IsDownloaded) ?? 0;

                StatusText.Text = $"Selected: {selectedCount} icons | Installed: {installedCount} icons";
                StorageText.Text = "Storage used: 0 KB";

                DownloadButton.IsEnabled = toDownloadCount > 0;
                DownloadButton.Content = toDownloadCount > 0 ? $"Download {toDownloadCount} Icons" : "Download Selected";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchBox.Text?.ToLowerInvariant() ?? "";

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredIcons = new List<IconManagementItem>(_allIcons);
                }
                else
                {
                    _filteredIcons = _allIcons.Where(icon =>
                        icon.DisplayName.ToLowerInvariant().Contains(searchText) ||
                        icon.Name.ToLowerInvariant().Contains(searchText) ||
                        icon.Category.ToLowerInvariant().Contains(searchText)
                    ).ToList();
                }

                UpdateIconList();
                InfoText.Text = _filteredIcons.Count == _allIcons.Count
                    ? "50 default icons included"
                    : $"Showing {_filteredIcons.Count} of {_allIcons.Count} icons";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering icons: {ex.Message}");
            }
        }

        private void IconCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkBox = sender as CheckBox;
                var iconName = checkBox?.Tag as string;

                if (iconName != null)
                {
                    var icon = _allIcons.FirstOrDefault(i => i.Name == iconName);
                    if (icon != null)
                    {
                        icon.IsSelected = checkBox.IsChecked == true;
                        icon.Status = icon.IsDownloaded ? "Downloaded" : (icon.IsSelected ? "Selected" : "");

                        // Update preferences
                        var selectedIcons = _allIcons.Where(i => i.IsSelected).Select(i => i.Name).ToList();
                        IconManager.UpdateSelectedIcons(selectedIcons);

                        UpdateStatusDisplay();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling checkbox change: {ex.Message}");
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DownloadButton.IsEnabled = false;
                DownloadButton.Content = "Downloading...";

                var result = await IconManager.DownloadSelectedIconsAsync();

                if (result.SuccessCount > 0)
                {
                    // Update downloaded status
                    foreach (var icon in _allIcons.Where(i => i.IsSelected && !i.IsDownloaded))
                    {
                        if (!result.FailedIcons.Contains(icon.Name))
                        {
                            icon.IsDownloaded = true;
                            icon.Status = "Downloaded";
                        }
                    }

                    UpdateIconList();
                    UpdateStatusDisplay();

                    var message = $"{result.SuccessCount} icon(s) downloaded successfully!";
                    if (result.FailedIcons.Count > 0)
                        message += $"\n{result.FailedIcons.Count} failed.";

                    MessageBox.Show(message, "Download Complete",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Download failed. Check internet connection.",
                                   "Download Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading: {ex.Message}");
                MessageBox.Show($"Download error: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
                UpdateStatusDisplay();
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}