using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace gtt_sidebar.Core.Settings
{
    public partial class IconPicker : Window
    {
        public event Action<string, string> IconSelected; // iconType, iconValue
        private bool _isClosing = false;

        public IconPicker()
        {
            InitializeComponent();
            LoadIcons();
        }

        /// <summary>
        /// Load all built-in Phosphor icons into the grid
        /// </summary>
        private void LoadIcons()
        {
            try
            {
                IconsGrid.Children.Clear();

                foreach (var iconName in IconCatalog.BuiltInIcons)
                {
                    var button = CreateIconButton(iconName);
                    IconsGrid.Children.Add(button);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {IconCatalog.BuiltInIcons.Count} Phosphor icons into picker");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icons: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a clickable button for a Phosphor icon
        /// </summary>
        private Button CreateIconButton(string iconName)
        {
            var button = new Button
            {
                Width = 26,
                Height = 26,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Tag = iconName,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                ToolTip = iconName.Replace("-", " ") // Show friendly name
            };

            // Try to load PNG icon from embedded resources first, then file system
            var iconPath = GetIconPath(iconName);
            if (!string.IsNullOrEmpty(iconPath))
            {
                // Show PNG image
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

                    if (iconPath.StartsWith("pack://"))
                    {
                        // Embedded resource
                        bitmap.UriSource = new Uri(iconPath);
                    }
                    else
                    {
                        // File system
                        bitmap.UriSource = new Uri(iconPath);
                    }

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;
                    button.Content = image;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading icon {iconName}: {ex.Message}");
                    // Fallback to text
                    button.Content = CreateTextFallback(iconName);
                }
            }
            else
            {
                // Fallback to abbreviated text
                button.Content = CreateTextFallback(iconName);
            }

            // Add hover effects
            button.MouseEnter += (s, e) => button.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
            button.MouseLeave += (s, e) => button.BorderBrush = Brushes.Transparent;

            button.Click += IconButton_Click;

            return button;
        }

        /// <summary>
        /// Create text fallback for missing PNG icons
        /// </summary>
        private TextBlock CreateTextFallback(string iconName)
        {
            var text = iconName.Length >= 2 ? iconName.Substring(0, 2).ToUpper() : iconName.ToUpper();

            return new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
            };
        }

        /// <summary>
        /// Get the path to a Phosphor icon PNG file - FIXED VERSION
        /// </summary>
        private string GetIconPath(string iconName)
        {
            try
            {
                // Method 1: Try embedded resource first
                var resourcePath = $"pack://application:,,,/Core/Icons/DefaultIcons/{iconName}.png";
                try
                {
                    var resourceStream = System.Windows.Application.GetResourceStream(new Uri(resourcePath)); if (resourceStream != null)
                    {
                        resourceStream.Stream.Close();
                        System.Diagnostics.Debug.WriteLine($"Found embedded resource: {iconName}");
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
                    // In project build output directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Icons", "DefaultIcons", $"{iconName}.png"),
                    
                    // In AppData (where other data is stored)
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gtt-sidebar", "DefaultIcons", $"{iconName}.png"),
                    
                    // Relative to executable
                    Path.Combine("Core", "Icons", "DefaultIcons", $"{iconName}.png"),
                    // Direct in executable directory
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultIcons", $"{iconName}.png"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gtt-sidebar", "icons", $"{iconName}.png"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gtt-sidebar", "icons", $"{iconName}.svg")

                    
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found file: {iconName} at {path}");
                        return path;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Not found at: {path}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"PNG not found for {iconName} in any location");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting icon path for {iconName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Handle icon button click
        /// </summary>
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var iconName = button?.Tag as string;

            if (!string.IsNullOrEmpty(iconName))
            {
                System.Diagnostics.Debug.WriteLine($"Selected Phosphor icon: {iconName}");
                IconSelected?.Invoke("builtin", iconName);
                _isClosing = true;
                this.Close();
            }
        }

        /// <summary>
        /// Handle custom icon button click
        /// </summary>
        private void CustomIconButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CustomIconButton_Click: Starting file dialog...");

                // Hide this window temporarily to prevent dialog issues
                this.Hide();

                OpenFileDialog openFileDialog = null;
                bool? result = false;
                string selectedFile = null;

                openFileDialog = new OpenFileDialog
                {
                    Title = "Select Custom Icon",
                    Filter = "PNG Files (*.png)|*.png|All Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico",
                    FilterIndex = 1,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false
                };

                result = openFileDialog.ShowDialog();
                selectedFile = openFileDialog.FileName;

                if (result == true && !string.IsNullOrWhiteSpace(selectedFile))
                {
                    System.Diagnostics.Debug.WriteLine($"Selected custom icon file: {selectedFile}");

                    // Generate a unique ID for this icon
                    var iconId = Guid.NewGuid().ToString();

                    // Process and save the custom icon
                    var savedFileName = ProcessCustomIcon(selectedFile, iconId);

                    if (!string.IsNullOrEmpty(savedFileName))
                    {
                        IconSelected?.Invoke("custom", savedFileName);
                        _isClosing = true;
                        this.Close();
                    }
                    else
                    {
                        // Show this window again and display error
                        this.Show();
                        MessageBox.Show("Failed to process the selected image file.", "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // User cancelled or no file selected - show window again
                    this.Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CustomIconButton_Click: {ex.Message}");

                // Make sure window is visible again
                if (!this.IsVisible && !_isClosing)
                {
                    this.Show();
                }

                MessageBox.Show($"Error selecting custom icon: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Process and resize a custom icon file to 32x32 pixels
        /// </summary>
        private string ProcessCustomIcon(string sourceFilePath, string iconId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing custom icon: {sourceFilePath}");

                if (!File.Exists(sourceFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Source file doesn't exist");
                    return null;
                }

                // Load and process the image
                BitmapSource processedImage = null;

                using (var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Create bitmap decoder
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];

                    // Create a resized version (32x32 for future-proofing)
                    var transform = new ScaleTransform(32.0 / frame.PixelWidth, 32.0 / frame.PixelHeight);
                    processedImage = new TransformedBitmap(frame, transform);
                }

                if (processedImage == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create processed image");
                    return null;
                }

                // Create PNG encoder and save
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(processedImage));

                var fileName = $"{iconId}.png";
                var savedPath = ShortcutsStorage.SaveCustomIconFromBitmap(encoder, fileName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully processed and saved: {fileName}");
                    return fileName;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing custom icon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Position the picker window near the specified point
        /// </summary>
        public void PositionNear(Point screenPoint)
        {
            // Position the window near the click point, but ensure it stays on screen
            var left = screenPoint.X - (this.Width / 2);
            var top = screenPoint.Y - this.Height - 10; // 10px above the click point

            // Ensure window stays on screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (left < 0) left = 0;
            if (left + this.Width > screenWidth) left = screenWidth - this.Width;
            if (top < 0) top = screenPoint.Y + 10; // Show below if no room above
            if (top + this.Height > screenHeight) top = screenHeight - this.Height;

            this.Left = left;
            this.Top = top;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        /// <summary>
        /// Handle window deactivation - close the picker
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                this.Close();
            }
        }
    }
}