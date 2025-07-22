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
        /// Load all built-in icons into the grid
        /// </summary>
        private void LoadIcons()
        {
            try
            {
                IconsGrid.Children.Clear();

                foreach (var icon in IconCatalog.BuiltInIcons)
                {
                    var button = CreateIconButton(icon);
                    IconsGrid.Children.Add(button);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {IconCatalog.BuiltInIcons.Count} icons into picker");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icons: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a clickable button for an icon
        /// </summary>
        private Button CreateIconButton(string icon)
        {
            var button = new Button
            {
                Width = 26,  // Increased from 24 to prevent cropping
                Height = 26, // Increased from 24 to prevent cropping
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Content = icon,
                FontSize = 18, // Match our widget icon size
                Cursor = Cursors.Hand,
                Tag = icon,
                HorizontalContentAlignment = HorizontalAlignment.Center,  // Center horizontally
                VerticalContentAlignment = VerticalAlignment.Center,      // Center vertically
                Padding = new Thickness(0) // Remove default button padding
            };

            // Add hover effects (no tooltip)
            button.MouseEnter += (s, e) => button.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
            button.MouseLeave += (s, e) => button.BorderBrush = Brushes.Transparent;

            button.Click += IconButton_Click;

            return button;
        }

        /// <summary>
        /// Handle icon button click
        /// </summary>
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var icon = button?.Tag as string;

            if (!string.IsNullOrEmpty(icon))
            {
                System.Diagnostics.Debug.WriteLine($"Selected built-in icon: {icon}");
                IconSelected?.Invoke("builtin", icon);
                _isClosing = true;
                this.Close();
            }
        }

        /// <summary>
        /// Handle custom icon button click (FIXED VERSION)
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
        /// Process and resize a custom icon file (SAFER VERSION)
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

                // Load and process the image in a safer way
                BitmapSource processedImage = null;

                using (var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Create bitmap decoder
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];

                    // Create a resized version (18x18)
                    var transform = new ScaleTransform(18.0 / frame.PixelWidth, 18.0 / frame.PixelHeight);
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