﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using gtt_sidebar.Core.Settings;
using gtt_sidebar.Core.Managers;

namespace gtt_sidebar.Core.Settings
{
    public partial class SettingsWindow : Window
    {
        public event Action ShortcutsChanged;
        public event Action SystemMonitorSettingsChanged;
        private SettingsData _originalSettings;
        private SettingsData _currentSettings;
        private ShortcutsData _shortcutsData;
        private Dictionary<string, Border> _shortcutCards = new Dictionary<string, Border>();
        private bool _isDragging = false;
        // Add these fields to the existing private fields in SettingsWindow class
        private Border _draggedCard = null;
        private Point _dragStartPoint;
        private int _draggedIndex = -1;
        private int _dropIndex = -1;

        public event Action<SettingsData> SettingsApplied;

        public SettingsWindow(SettingsData currentSettings)
        {
            InitializeComponent();

            _originalSettings = currentSettings;
            _currentSettings = CloneSettings(currentSettings);

            LoadSettingsToUI();
            LoadShortcutsData();
        }

        // Add these methods to SettingsWindow.xaml.cs

        /// <summary>
        /// Initialize shortcuts data when settings window opens
        /// </summary>
        private void LoadShortcutsData()
        {
            try
            {
                _shortcutsData = ShortcutsStorage.LoadShortcuts();
                LoadShortcutsToUI();
                //System.Diagnostics.Debug.WriteLine($"Loaded {_shortcutsData.Shortcuts.Count} shortcuts to settings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading shortcuts data: {ex.Message}");
                _shortcutsData = ShortcutsData.CreateDefault();
                LoadShortcutsToUI();
            }
        }

        /// <summary>
        /// Validate shortcut data before saving
        /// </summary>
        private bool ValidateShortcutData(ShortcutItem shortcut, out string errorMessage)
        {
            errorMessage = "";

            // Check label
            if (string.IsNullOrWhiteSpace(shortcut.Label))
            {
                errorMessage = "Label cannot be empty.";
                return false;
            }

            // Check path
            if (string.IsNullOrWhiteSpace(shortcut.Path))
            {
                errorMessage = "Path cannot be empty.";
                return false;
            }

            // Type-specific validation
            switch (shortcut.Type)
            {
                case ShortcutType.URL:
                    if (!shortcut.Path.StartsWith("http://") && !shortcut.Path.StartsWith("https://") &&
                        !shortcut.Path.StartsWith("ftp://") && !shortcut.Path.StartsWith("file://"))
                    {
                        errorMessage = "URL must start with http://, https://, ftp://, or file://";
                        return false;
                    }
                    break;

                case ShortcutType.Executable:
                case ShortcutType.WindowsShortcut:
                    if (!File.Exists(shortcut.Path))
                    {
                        errorMessage = $"File not found: {shortcut.Path}\n\nPlease check the path.";
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Creates UI cards for all shortcuts
        /// </summary>
        private void LoadShortcutsToUI()
        {
            ShortcutsContainer.Children.Clear();
            _shortcutCards.Clear();

            if (_shortcutsData?.Shortcuts == null)
                return;

            foreach (var shortcut in _shortcutsData.Shortcuts.OrderBy(s => s.Order))
            {
                var card = CreateShortcutCard(shortcut);
                ShortcutsContainer.Children.Add(card);
                _shortcutCards[shortcut.Id] = card;
            }

            //System.Diagnostics.Debug.WriteLine($"Created UI for {_shortcutCards.Count} shortcuts");
        }

        /// <summary>
        /// Create icon display for shortcut card - UPDATED WITH PNG SUPPORT
        /// </summary>
        private UIElement CreateIconDisplay(ShortcutItem shortcut)
        {
            if (shortcut.IconType == "custom")
            {
                // Show custom image
                var image = new Image
                {
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };

                var iconPath = ShortcutsStorage.GetCustomIconPath(shortcut.IconValue);
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(iconPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        image.Source = bitmap;
                        return image;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading custom icon in settings: {ex.Message}");
                    }
                }

                // Fallback for custom icons
                return new TextBlock
                {
                    Text = "📁",
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
            }
            else
            {
                // Try to load PNG for built-in icon
                var iconPath = GetBuiltinIconPath(shortcut.IconValue);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var image = new Image
                    {
                        Width = 18,
                        Height = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    };

                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(iconPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        image.Source = bitmap;
                        return image;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading builtin icon PNG in settings {shortcut.IconValue}: {ex.Message}");
                    }
                }

                // Fallback to text
                return new TextBlock
                {
                    Text = shortcut.IconValue,
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };
            }
        }

        /// <summary>
        /// Get path to built-in Phosphor icon PNG - copied from IconPicker
        /// </summary>
        private string GetBuiltinIconPath(string iconName)
        {
            try
            {
                // Method 1: Try embedded resource first
                var resourcePath = $"pack://application:,,,/Core/Icons/DefaultIcons/{iconName}.png";
                try
                {
                    var resourceStream = System.Windows.Application.GetResourceStream(new Uri(resourcePath));
                    if (resourceStream != null)
                    {
                        resourceStream.Stream.Close();
                        return resourcePath;
                    }
                }
                catch (Exception)
                {
                    // Not an embedded resource, try file system
                }

                // Method 2: Try file system locations
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Icons", "DefaultIcons", $"{iconName}.png"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gtt-sidebar", "DefaultIcons", $"{iconName}.png"),
                    Path.Combine("Core", "Icons", "DefaultIcons", $"{iconName}.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultIcons", $"{iconName}.png")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting builtin icon path for {iconName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a UI card for a single shortcut with up/down reordering buttons
        /// </summary>
        private Border CreateShortcutCard(ShortcutItem shortcut)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8),
                Tag = shortcut.Id
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Buttons

            // Icon Preview (Clickable)
            var iconBorder = new Border
            {
                Width = 26,
                Height = 26,
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Tag = shortcut.Id,
                Background = new SolidColorBrush(Color.FromRgb(248, 248, 248))
            };

            var iconDisplay = CreateIconDisplay(shortcut);
            iconBorder.Child = iconDisplay;
            iconBorder.PreviewMouseLeftButtonDown += IconBorder_Click;

            // Content Area
            var contentPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Label row
            var labelGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            labelGrid.Children.Add(new TextBlock
            {
                Text = "Label:",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                HorizontalAlignment = HorizontalAlignment.Left
            });

            var labelTextBox = new TextBox
            {
                Text = shortcut.Label,
                FontSize = 9,
                Height = 20,
                Margin = new Thickness(35, 0, 0, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                Tag = shortcut.Id
            };

            labelTextBox.TextChanged += ShortcutField_TextChanged;
            labelGrid.Children.Add(labelTextBox);
            contentPanel.Children.Add(labelGrid);

            // Path row
            var pathGrid = new Grid();
            pathGrid.Children.Add(new TextBlock
            {
                Text = "Path:",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                HorizontalAlignment = HorizontalAlignment.Left
            });

            var pathTextBox = new TextBox
            {
                Text = shortcut.Path,
                FontSize = 9,
                Height = 20,
                Margin = new Thickness(35, 0, 0, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                Tag = shortcut.Id
            };
            pathTextBox.TextChanged += ShortcutField_TextChanged;
            pathGrid.Children.Add(pathTextBox);
            contentPanel.Children.Add(pathGrid);

            // Buttons Panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            // Up Button
            var upButton = new Button
            {
                Content = "▲",
                Width = 20,
                Height = 20,
                FontSize = 8,
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 2, 0),
                ToolTip = "Move up",
                Tag = shortcut.Id
            };
            upButton.Click += MoveUp_Click;

            // Down Button
            var downButton = new Button
            {
                Content = "▼",
                Width = 20,
                Height = 20,
                FontSize = 8,
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Move down",
                Tag = shortcut.Id
            };
            downButton.Click += MoveDown_Click;

            // Save Button
            var saveButton = new Button
            {
                Content = "💾",
                Width = 24,
                Height = 24,
                FontSize = 10,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 4, 0),
                IsEnabled = false,
                ToolTip = "Save changes",
                Tag = shortcut.Id
            };
            saveButton.Click += SaveShortcut_Click;

            // Delete Button
            var deleteButton = new Button
            {
                Content = "×",
                Width = 24,
                Height = 24,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                ToolTip = "Delete shortcut",
                Tag = shortcut.Id
            };
            deleteButton.Click += DeleteShortcut_Click;

            buttonsPanel.Children.Add(upButton);
            buttonsPanel.Children.Add(downButton);
            buttonsPanel.Children.Add(saveButton);
            buttonsPanel.Children.Add(deleteButton);

            // Assemble grid
            Grid.SetColumn(iconBorder, 0);
            Grid.SetColumn(contentPanel, 1);
            Grid.SetColumn(buttonsPanel, 2);
            grid.Children.Add(iconBorder);
            grid.Children.Add(contentPanel);
            grid.Children.Add(buttonsPanel);

            card.Child = grid;

            // Add hover effects for the icon border
            iconBorder.MouseEnter += (s, e) => iconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
            iconBorder.MouseLeave += (s, e) => iconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208));

            return card;
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var shortcutId = ((Button)sender).Tag.ToString();
            var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            var index = _shortcutsData.Shortcuts.IndexOf(shortcut);

            if (index > 0)
            {
                _shortcutsData.Shortcuts.RemoveAt(index);
                _shortcutsData.Shortcuts.Insert(index - 1, shortcut);
                UpdateOrders();
                SharedResourceManager.Instance.QueueSave("shortcuts", _shortcutsData);
                LoadShortcutsToUI();
                ShortcutsChanged?.Invoke();
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var shortcutId = ((Button)sender).Tag.ToString();
            var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            var index = _shortcutsData.Shortcuts.IndexOf(shortcut);

            if (index < _shortcutsData.Shortcuts.Count - 1)
            {
                _shortcutsData.Shortcuts.RemoveAt(index);
                _shortcutsData.Shortcuts.Insert(index + 1, shortcut);
                UpdateOrders();
                SharedResourceManager.Instance.QueueSave("shortcuts", _shortcutsData);
                LoadShortcutsToUI();
                ShortcutsChanged?.Invoke();
            }
        }

        private void UpdateOrders()
        {
            for (int i = 0; i < _shortcutsData.Shortcuts.Count; i++)
                _shortcutsData.Shortcuts[i].Order = i;
        }

        // Add these event handlers to SettingsWindow.xaml.cs

        /// <summary>
        /// Handle shortcuts section collapse/expand
        /// </summary>
        private void ShortcutsHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (ShortcutsContent.Visibility == Visibility.Visible)
            {
                ShortcutsContent.Visibility = Visibility.Collapsed;
                ShortcutsArrow.Text = "▶";
            }
            else
            {
                ShortcutsContent.Visibility = Visibility.Visible;
                ShortcutsArrow.Text = "▼";
            }
        }

        /// <summary>
        /// Handle text changes in shortcut fields
        /// </summary>
        private void ShortcutField_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Tag == null) return;

            var shortcutId = textBox.Tag.ToString();

            // Find the save button for this shortcut and enable it
            var card = _shortcutCards.ContainsKey(shortcutId) ? _shortcutCards[shortcutId] : null;

            if (card != null)
            {
                var saveButton = FindChildByTag<Button>(card, shortcutId);
                if (saveButton != null && saveButton.ToolTip?.ToString() == "Save changes")
                {
                    saveButton.IsEnabled = true;
                    saveButton.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green when enabled
                }
            }
        }
        /// <summary>
        /// Handle icon border click - show icon picker (with drag prevention)
        /// </summary>
        private void IconBorder_Click(object sender, MouseButtonEventArgs e)
        {
            // Stop the event from bubbling up to drag handler
            e.Handled = true;
            e.Handled = true; // Sometimes needed twice for WPF

            var border = sender as Border;
            if (border?.Tag == null) return;

            var shortcutId = border.Tag.ToString();
            var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            if (shortcut == null) return;

            try
            {
                // Get screen position of the clicked icon
                var screenPoint = border.PointToScreen(new Point(border.ActualWidth / 2, 0));

                // Create and show icon picker
                var iconPicker = new IconPicker();
                iconPicker.IconSelected += (iconType, iconValue) => OnIconSelected(shortcutId, iconType, iconValue);
                iconPicker.PositionNear(screenPoint);
                iconPicker.Show();

                System.Diagnostics.Debug.WriteLine($"Opened icon picker for shortcut: {shortcut.Label}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening icon picker: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle icon selection from picker
        /// </summary>
        private void OnIconSelected(string shortcutId, string iconType, string iconValue)
        {
            try
            {
                var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
                if (shortcut == null) return;

                System.Diagnostics.Debug.WriteLine($"OnIconSelected: {iconType} = {iconValue} for {shortcut.Label}");

                // Update shortcut data
                shortcut.IconType = iconType;
                shortcut.IconValue = iconValue;

                // Update the visual immediately
                if (_shortcutCards.ContainsKey(shortcutId))
                {
                    var card = _shortcutCards[shortcutId];

                    // Find the icon border by walking the visual tree
                    var iconBorder = FindIconBorderInCard(card);
                    if (iconBorder != null)
                    {
                        // COMPLETELY RECREATE THE ICON DISPLAY
                        UIElement newIconDisplay;

                        if (iconType == "custom")
                        {
                            // Create new image for custom icon
                            var image = new Image
                            {
                                Width = 18,
                                Height = 18,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                IsHitTestVisible = false
                            };

                            // Load the custom icon
                            var iconPath = ShortcutsStorage.GetCustomIconPath(iconValue);
                            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                            {
                                try
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(iconPath);
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    image.Source = bitmap;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error loading updated custom icon: {ex.Message}");
                                }
                            }

                            newIconDisplay = image;
                        }
                        else
                        {
                            // Create new TextBlock for built-in icon
                            var iconText = new TextBlock
                            {
                                Text = iconValue,
                                FontSize = 18,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                IsHitTestVisible = false,
                                Padding = new Thickness(0),
                                Margin = new Thickness(0)
                            };
                            newIconDisplay = iconText;
                        }

                        // Replace the old content with new content
                        // Update the visual using the same logic as card creation
                        var updatedIconDisplay = CreateIconDisplay(shortcut);
                        iconBorder.Child = updatedIconDisplay;
                        System.Diagnostics.Debug.WriteLine($"Updated icon display to: {iconType} = {iconValue}");
                    }

                    // Enable save button
                    EnableSaveButtonForCard(shortcutId);
                }

                System.Diagnostics.Debug.WriteLine($"Icon selection complete for '{shortcut.Label}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnIconSelected: {ex.Message}");
                MessageBox.Show($"Error updating icon: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        /// <summary>
        /// Helper to find icon border in a card
        /// </summary>
        private Border FindIconBorderInCard(Border card)
        {
            // The icon border is the first Border child in the card's grid
            var grid = card.Child as Grid;
            if (grid?.Children.Count > 0 && grid.Children[0] is Border iconBorder)
            {
                return iconBorder;
            }
            return null;
        }

        /// <summary>
        /// Enable save button for a specific card
        /// </summary>
        private void EnableSaveButtonForCard(string shortcutId)
        {
            if (_shortcutCards.ContainsKey(shortcutId))
            {
                var card = _shortcutCards[shortcutId];
                var saveButton = FindSaveButtonInCard(card);
                if (saveButton != null)
                {
                    saveButton.IsEnabled = true;
                    saveButton.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                }
            }
        }

        /// <summary>
        /// Helper to find save button in a card
        /// </summary>
        private Button FindSaveButtonInCard(Border card)
        {
            // Walk the visual tree to find the save button (has disk emoji)
            return FindChildByContent<Button>(card, "💾");
        }

        /// <summary>
        /// Find child by content
        /// </summary>
        private T FindChildByContent<T>(DependencyObject parent, string content) where T : ContentControl
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Content?.ToString() == content)
                    return element;

                var found = FindChildByContent<T>(child, content);
                if (found != null)
                    return found;
            }
            return null;
        }
        /// <summary>
        /// Handle save button click
        /// </summary>
        private void SaveShortcut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            var shortcutId = button.Tag.ToString();
            var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            if (shortcut == null) return;

            try
            {
                var card = _shortcutCards.ContainsKey(shortcutId) ? _shortcutCards[shortcutId] : null;
                if (card != null)
                {
                    // Find text boxes by walking the visual tree
                    var labelTextBox = FindTextBoxInCard(card, 0); // First text box (label)
                    var pathTextBox = FindTextBoxInCard(card, 1);  // Second text box (path)

                    if (labelTextBox != null && pathTextBox != null)
                    {
                        shortcut.Label = labelTextBox.Text?.Trim() ?? "";
                        shortcut.Path = pathTextBox.Text?.Trim() ?? "";
                        shortcut.Type = ShortcutItem.DetectType(shortcut.Path); // Re-detect type

                        // Validate the shortcut
                        string validationError;
                        if (!ValidateShortcutData(shortcut, out validationError))
                        {
                            MessageBox.Show(validationError, "Validation Error",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Save to storage
                        SharedResourceManager.Instance.QueueSave("shortcuts", _shortcutsData);

                        // Disable save button
                        button.IsEnabled = false;
                        button.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gray when disabled
                        ShortcutsChanged?.Invoke();
                        System.Diagnostics.Debug.WriteLine($"Saved shortcut: {shortcut.Label} -> {shortcut.Path}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving shortcut: {ex.Message}");
                MessageBox.Show($"Error saving shortcut: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle delete button click
        /// </summary>
        private void DeleteShortcut_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            var shortcutId = button.Tag.ToString();
            var shortcut = _shortcutsData.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            if (shortcut == null) return;

            // Confirmation dialog
            var result = MessageBox.Show(
                $"Are you sure you want to delete the shortcut '{shortcut.Label}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Remove from data
                    _shortcutsData.RemoveShortcut(shortcutId);

                    // Save to storage
                    SharedResourceManager.Instance.QueueSave("shortcuts", _shortcutsData);

                    // Remove from UI
                    if (_shortcutCards.ContainsKey(shortcutId))
                    {
                        ShortcutsContainer.Children.Remove(_shortcutCards[shortcutId]);
                        _shortcutCards.Remove(shortcutId);
                    }
                    ShortcutsChanged?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"Deleted shortcut: {shortcut.Label}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting shortcut: {ex.Message}");
                    MessageBox.Show($"Error deleting shortcut: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handle add new shortcut button click
        /// </summary>
        private void AddShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new shortcut with default values
                var newShortcut = _shortcutsData.AddShortcut("New Shortcut", "calc", "builtin", "📁");

                // Save to storage
                SharedResourceManager.Instance.QueueSave("shortcuts", _shortcutsData);

                // Add to UI
                var card = CreateShortcutCard(newShortcut);
                ShortcutsContainer.Children.Add(card);
                _shortcutCards[newShortcut.Id] = card;

                ShortcutsChanged?.Invoke();

                // Focus on the label text box for immediate editing
                var labelTextBox = FindTextBoxInCard(card, 0); // First text box
                labelTextBox?.Focus();
                labelTextBox?.SelectAll();

                System.Diagnostics.Debug.WriteLine($"Added new shortcut with ID: {newShortcut.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding shortcut: {ex.Message}");
                MessageBox.Show($"Error adding shortcut: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Helper method to find child controls by type and tag
        /// </summary>
        private T FindChildByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Tag?.ToString() == tag)
                    return element;

                var found = FindChildByTag<T>(child, tag);
                if (found != null)
                    return found;
            }
            return null;
        }

  
        
        


        private SettingsData CloneSettings(SettingsData settings)
        {
            return new SettingsData
            {
                Window = new WindowSettings
                {
                    Position = settings.Window.Position,
                    Width = settings.Window.Width,
                    MarginTop = settings.Window.MarginTop,
                    MarginBottom = settings.Window.MarginBottom,
                    MarginSide = settings.Window.MarginSide
                },
                SystemMonitor = new SystemMonitorSettings
                {
                    CpuThreshold = settings.SystemMonitor.CpuThreshold,
                    RamThreshold = settings.SystemMonitor.RamThreshold,
                    PingThreshold = settings.SystemMonitor.PingThreshold,
                    UpdateFrequencySeconds = settings.SystemMonitor.UpdateFrequencySeconds
                },
                Version = settings.Version
            };
        }

        #region System Monitor Settings

        private void SystemMonitorHeader_Click(object sender, MouseButtonEventArgs e)
        {
            // Toggle section visibility
            if (SystemMonitorContent.Visibility == Visibility.Visible)
            {
                SystemMonitorContent.Visibility = Visibility.Collapsed;
                SystemMonitorToggle.Text = "▶";
            }
            else
            {
                SystemMonitorContent.Visibility = Visibility.Visible;
                SystemMonitorToggle.Text = "▼";
            }
        }

        private void CpuThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CpuThresholdValue != null)
            {
                CpuThresholdValue.Text = $"{(int)e.NewValue}%";
            }
        }

        private void RamThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RamThresholdValue != null)
            {
                RamThresholdValue.Text = $"{(int)e.NewValue}%";
            }
        }

        private void PingThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PingThresholdValue != null)
            {
                PingThresholdValue.Text = $"{(int)e.NewValue}ms";
            }
        }

        private void UpdateFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Nothing special needed here - just for UI feedback
        }

        #endregion

        private void LoadSettingsToUI()
        {
            // Position
            RightPositionRadio.IsChecked = _currentSettings.Window.Position == SidebarPosition.Right;
            LeftPositionRadio.IsChecked = _currentSettings.Window.Position == SidebarPosition.Left;

            // Width
            WidthSlider.Value = _currentSettings.Window.Width;
            WidthValueText.Text = $"{_currentSettings.Window.Width:F0} px";

            // Margins
            MarginTopSlider.Value = _currentSettings.Window.MarginTop;
            MarginTopValueText.Text = $"{_currentSettings.Window.MarginTop:F0} px";

            MarginBottomSlider.Value = _currentSettings.Window.MarginBottom;
            MarginBottomValueText.Text = $"{_currentSettings.Window.MarginBottom:F0} px";

            MarginSideSlider.Value = _currentSettings.Window.MarginSide;
            MarginSideValueText.Text = $"{_currentSettings.Window.MarginSide:F0} px";

            LoadSystemMonitorSettingsToUI();
        }

        private void LoadSystemMonitorSettingsToUI()
        {
            try
            {
                // Load System Monitor settings (only if the controls exist)
                if (CpuThresholdSlider != null)
                {
                    CpuThresholdSlider.Value = _currentSettings.SystemMonitor.CpuThreshold;
                    RamThresholdSlider.Value = _currentSettings.SystemMonitor.RamThreshold;
                    PingThresholdSlider.Value = _currentSettings.SystemMonitor.PingThreshold;

                    // Set update frequency
                    foreach (ComboBoxItem item in UpdateFrequencyComboBox.Items)
                    {
                        if (item.Tag.ToString() == _currentSettings.SystemMonitor.UpdateFrequencySeconds.ToString())
                        {
                            UpdateFrequencyComboBox.SelectedItem = item;
                            break;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Settings: Loaded System Monitor settings - CPU: {_currentSettings.SystemMonitor.CpuThreshold}%, RAM: {_currentSettings.SystemMonitor.RamThreshold}%, Ping: {_currentSettings.SystemMonitor.PingThreshold}ms, Freq: {_currentSettings.SystemMonitor.UpdateFrequencySeconds}s");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings: Error loading System Monitor settings: {ex.Message}");
            }
        }

        private void UpdateCurrentSettings()
        {
            // Position
            _currentSettings.Window.Position = RightPositionRadio.IsChecked == true
                ? SidebarPosition.Right
                : SidebarPosition.Left;

            // Width
            _currentSettings.Window.Width = WidthSlider.Value;

            // Margins
            _currentSettings.Window.MarginTop = MarginTopSlider.Value;
            _currentSettings.Window.MarginBottom = MarginBottomSlider.Value;
            _currentSettings.Window.MarginSide = MarginSideSlider.Value;

            UpdateSystemMonitorSettings();

        }
        private void UpdateSystemMonitorSettings()
        {
            try
            {
                if (CpuThresholdSlider != null)
                {
                    // Save threshold values
                    _currentSettings.SystemMonitor.CpuThreshold = (int)CpuThresholdSlider.Value;
                    _currentSettings.SystemMonitor.RamThreshold = (int)RamThresholdSlider.Value;
                    _currentSettings.SystemMonitor.PingThreshold = (int)PingThresholdSlider.Value;

                    // Save update frequency
                    if (UpdateFrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        _currentSettings.SystemMonitor.UpdateFrequencySeconds = int.Parse(selectedItem.Tag.ToString());
                    }

                    System.Diagnostics.Debug.WriteLine($"Settings: Updated System Monitor settings - CPU: {_currentSettings.SystemMonitor.CpuThreshold}%, RAM: {_currentSettings.SystemMonitor.RamThreshold}%, Ping: {_currentSettings.SystemMonitor.PingThreshold}ms, Freq: {_currentSettings.SystemMonitor.UpdateFrequencySeconds}s");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings: Error updating System Monitor settings: {ex.Message}");
            }
        }

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WidthValueText != null)
            {
                WidthValueText.Text = $"{e.NewValue:F0} px";
                ValidateCurrentValues();
            }
        }

        private void MarginTopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MarginTopValueText != null)
            {
                MarginTopValueText.Text = $"{e.NewValue:F0} px";
                ValidateCurrentValues();
            }
        }

        private void MarginBottomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MarginBottomValueText != null)
            {
                MarginBottomValueText.Text = $"{e.NewValue:F0} px";
                ValidateCurrentValues();
            }
        }

        private void MarginSideSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MarginSideValueText != null)
            {
                MarginSideValueText.Text = $"{e.NewValue:F0} px";
                ValidateCurrentValues();
            }
        }

        private void ValidateCurrentValues()
        {
            // Ensure all sliders are initialized
            if (WidthSlider == null || MarginTopSlider == null ||
                MarginBottomSlider == null || MarginSideSlider == null ||
                ValidationText == null || ApplyButton == null)
                return;

            var hasErrors = false;
            var errorMessage = "";

            // Check width
            if (WidthSlider.Value < 100 || WidthSlider.Value > 200)
            {
                hasErrors = true;
                errorMessage = "Width must be between 100-200 pixels";
            }

            // Check margins
            if (MarginTopSlider.Value < 0 || MarginTopSlider.Value > 50 ||
                MarginBottomSlider.Value < 0 || MarginBottomSlider.Value > 50 ||
                MarginSideSlider.Value < 0 || MarginSideSlider.Value > 50)
            {
                hasErrors = true;
                errorMessage = "Margins must be between 0-50 pixels";
            }

            if (hasErrors)
            {
                ValidationText.Text = errorMessage;
                ValidationText.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
            }
            else
            {
                ValidationText.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentSettings();

            // Validate settings
            if (!SettingsStorage.ValidateSettings(_currentSettings))
            {
                MessageBox.Show("Invalid settings. Please check your values.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Apply settings via SharedResourceManager (this will save to file and notify subscribers)
            SettingsApplied?.Invoke(_currentSettings);

            // TRIGGER SHORTCUTS REFRESH TOO
            ShortcutsChanged?.Invoke();

            // Remove direct SystemMonitorSettingsChanged call since it's now handled by SharedResourceManager
            // SystemMonitorSettingsChanged?.Invoke(); // REMOVE THIS LINE

            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton_Click(sender, e);
        }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        /// <summary>
        /// Helper to find text boxes in order within a card
        /// </summary>
        private TextBox FindTextBoxInCard(DependencyObject parent, int index)
        {
            var textBoxes = new List<TextBox>();
            FindTextBoxesRecursive(parent, textBoxes);
            return index < textBoxes.Count ? textBoxes[index] : null;
        }

        private void FindTextBoxesRecursive(DependencyObject parent, List<TextBox> textBoxes)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox textBox)
                    textBoxes.Add(textBox);

                FindTextBoxesRecursive(child, textBoxes);
            }
        }
        private void WindowLayoutHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (WindowLayoutContent.Visibility == Visibility.Visible)
            {
                WindowLayoutContent.Visibility = Visibility.Collapsed;
                WindowLayoutArrow.Text = "▶";
            }
            else
            {
                WindowLayoutContent.Visibility = Visibility.Visible;
                WindowLayoutArrow.Text = "▼";
            }
        }
    }
}