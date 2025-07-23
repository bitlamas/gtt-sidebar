using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.Extended.Svg;

namespace gtt_sidebar.Core.Settings
{
    /// <summary>
    /// Manages downloadable Phosphor icons from the full catalog
    /// </summary>
    public static class IconManager
    {
        private static readonly string _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "gtt-sidebar");

        private static readonly string _catalogFilePath = Path.Combine(_appDataPath, "phosphor-catalog.json");
        private static readonly string _preferencesFilePath = Path.Combine(_appDataPath, "icon-preferences.json");
        private static readonly string _downloadedIconsPath = Path.Combine(_appDataPath, "icons");

        private static PhosphorCatalog _catalog;
        private static IconPreferences _preferences;
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Initialize the icon manager
        /// </summary>
        public static async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(_appDataPath);
                Directory.CreateDirectory(_downloadedIconsPath);

                await LoadOrCreateCatalog();
                LoadPreferences();

                System.Diagnostics.Debug.WriteLine($"IconManager: Initialized with {_catalog.Icons.Count} available icons");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IconManager initialization error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load the Phosphor catalog from embedded resource or create default
        /// </summary>
        private static async Task LoadOrCreateCatalog()
        {
            try
            {
                // Try to load from embedded resource first
                var resourceUri = new Uri("pack://application:,,,/Core/Icons/phosphor_catalog.json");
                var resourceStream = System.Windows.Application.GetResourceStream(resourceUri);

                if (resourceStream != null)
                {
                    using (var reader = new StreamReader(resourceStream.Stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        _catalog = JsonConvert.DeserializeObject<PhosphorCatalog>(json);
                        System.Diagnostics.Debug.WriteLine("Loaded catalog from embedded resource");
                        return;
                    }
                }

                // Fallback to file system
                if (File.Exists(_catalogFilePath))
                {
                    var json = File.ReadAllText(_catalogFilePath);
                    _catalog = JsonConvert.DeserializeObject<PhosphorCatalog>(json);
                }
                else
                {
                    // Create minimal catalog with your 50 defaults marked as built-in
                    _catalog = CreateDefaultCatalog();
                    SaveCatalog();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading catalog: {ex.Message}");
                _catalog = CreateDefaultCatalog();
            }
        }

        private static PhosphorCatalog CreateDefaultCatalog()
        {
            var catalog = new PhosphorCatalog
            {
                Version = "2.1.1",
                LastUpdated = DateTime.Now,
                TotalIcons = 100, // 50 defaults + 50 additional
                DefaultIcons = IconCatalog.BuiltInIcons.ToList(),
                Icons = new List<PhosphorIconInfo>()
            };

            // Add your 50 defaults (marked as IsDefault = true)
            foreach (var iconName in IconCatalog.BuiltInIcons)
            {
                catalog.Icons.Add(new PhosphorIconInfo
                {
                    Name = iconName,
                    DisplayName = iconName.Replace("-", " ").ToTitleCase(),
                    Category = "default",
                    Tags = new List<string> { "default" },
                    DownloadUrl = $"https://cdn.jsdelivr.net/npm/@phosphor-icons/core@2.1.1/assets/regular/{iconName}.svg",
                    IsDefault = true,
                    Order = Array.IndexOf(IconCatalog.BuiltInIcons.ToArray(), iconName)
                });
            }

            // Add 50 additional downloadable icons
            var additionalIcons = new[]
            {
        new { name = "activity", display = "Activity", category = "communication" },
        new { name = "bluetooth", display = "Bluetooth", category = "connectivity" },
        new { name = "wifi", display = "WiFi", category = "connectivity" },
        new { name = "battery", display = "Battery", category = "system" },
        new { name = "shield", display = "Shield", category = "security" },
        new { name = "lock", display = "Lock", category = "security" },
        new { name = "key", display = "Key", category = "security" },
        new { name = "camera", display = "Camera", category = "media" },
        new { name = "microphone", display = "Microphone", category = "media" },
        new { name = "headphones", display = "Headphones", category = "media" },
        new { name = "speaker-high", display = "Speaker High", category = "media" },
        new { name = "video-camera", display = "Video Camera", category = "media" },
        new { name = "music-note", display = "Music Note", category = "media" },
        new { name = "play", display = "Play", category = "media" },
        new { name = "pause", display = "Pause", category = "media" },
        new { name = "stop", display = "Stop", category = "media" },
        new { name = "calendar-blank", display = "Calendar", category = "productivity" },
        new { name = "clock", display = "Clock", category = "productivity" },
        new { name = "timer", display = "Timer", category = "productivity" },
        new { name = "alarm", display = "Alarm", category = "productivity" },
        new { name = "bell", display = "Bell", category = "productivity" },
        new { name = "map-pin", display = "Map Pin", category = "location" },
        new { name = "compass", display = "Compass", category = "location" },
        new { name = "navigation-arrow", display = "Navigation", category = "location" },
        new { name = "airplane", display = "Airplane", category = "travel" },
        new { name = "car", display = "Car", category = "travel" },
        new { name = "bicycle", display = "Bicycle", category = "travel" },
        new { name = "train", display = "Train", category = "travel" },
        new { name = "bus", display = "Bus", category = "travel" },
        new { name = "house", display = "House", category = "places" },
        new { name = "office-chair", display = "Office", category = "places" },
        new { name = "storefront", display = "Store", category = "places" },
        new { name = "hospital", display = "Hospital", category = "places" },
        new { name = "graduation-cap", display = "Education", category = "places" },
        new { name = "heart", display = "Heart", category = "health" },
        new { name = "thermometer", display = "Thermometer", category = "health" },
        new { name = "pill", display = "Pill", category = "health" },
        new { name = "lightning", display = "Lightning", category = "weather" },
        new { name = "sun", display = "Sun", category = "weather" },
        new { name = "moon", display = "Moon", category = "weather" },
        new { name = "cloud-rain", display = "Rain", category = "weather" },
        new { name = "snowflake", display = "Snow", category = "weather" },
        new { name = "fire", display = "Fire", category = "elements" },
        new { name = "drop", display = "Water Drop", category = "elements" },
        new { name = "leaf", display = "Leaf", category = "nature" },
        new { name = "tree", display = "Tree", category = "nature" },
        new { name = "flower", display = "Flower", category = "nature" },
        new { name = "butterfly", display = "Butterfly", category = "nature" },
        new { name = "dog", display = "Dog", category = "animals" },
        new { name = "cat", display = "Cat", category = "animals" }
    };

            for (int i = 0; i < additionalIcons.Length; i++)
            {
                var icon = additionalIcons[i];
                catalog.Icons.Add(new PhosphorIconInfo
                {
                    Name = icon.name,
                    DisplayName = icon.display,
                    Category = icon.category,
                    Tags = new List<string> { icon.category },
                    DownloadUrl = $"https://cdn.jsdelivr.net/npm/@phosphor-icons/core@2.1.1/assets/regular/{icon.name}.svg",
                    IsDefault = false,
                    Order = 50 + i
                });
            }

            return catalog;
        }

        /// <summary>
        /// Load user preferences
        /// </summary>
        private static void LoadPreferences()
        {
            try
            {
                if (File.Exists(_preferencesFilePath))
                {
                    var json = File.ReadAllText(_preferencesFilePath);
                    _preferences = JsonConvert.DeserializeObject<IconPreferences>(json);
                }
                else
                {
                    _preferences = new IconPreferences
                    {
                        CatalogVersion = _catalog?.Version ?? "2.1.1",
                        SelectedIcons = new List<string>(),
                        DownloadedIcons = new List<string>()
                    };
                    SavePreferences();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preferences: {ex.Message}");
                _preferences = new IconPreferences();
            }
        }

        /// <summary>
        /// Save catalog to file
        /// </summary>
        private static void SaveCatalog()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_catalog, Formatting.Indented);
                File.WriteAllText(_catalogFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving catalog: {ex.Message}");
            }
        }

        /// <summary>
        /// Save user preferences
        /// </summary>
        private static void SavePreferences()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_preferences, Formatting.Indented);
                File.WriteAllText(_preferencesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all available icons (default + downloaded)
        /// </summary>
        public static List<string> GetAvailableIcons()
        {
            var available = new List<string>();

            // Add default icons (always available)
            available.AddRange(IconCatalog.BuiltInIcons);

            // Add downloaded icons
            if (_preferences?.DownloadedIcons != null)
            {
                available.AddRange(_preferences.DownloadedIcons.Where(icon => !available.Contains(icon)));
            }

            return available.Distinct().ToList();
        }

        /// <summary>
        /// Get catalog icons for management UI
        /// </summary>
        public static List<IconManagementItem> GetCatalogIcons()
        {
            if (_catalog?.Icons == null) return new List<IconManagementItem>();

            return _catalog.Icons
                .Where(icon => !icon.IsDefault) // Exclude your 50 defaults
                .Select(icon => new IconManagementItem
                {
                    Name = icon.Name,
                    DisplayName = icon.DisplayName,
                    Category = icon.Category,
                    IsSelected = _preferences?.SelectedIcons?.Contains(icon.Name) ?? false,
                    IsDownloaded = _preferences?.DownloadedIcons?.Contains(icon.Name) ?? false,
                    Status = GetIconStatus(icon.Name)
                })
                .OrderBy(item => item.Category)
                .ThenBy(item => item.DisplayName)
                .ToList();
        }

        /// <summary>
        /// Get icon status text
        /// </summary>
        private static string GetIconStatus(string iconName)
        {
            if (_preferences?.DownloadedIcons?.Contains(iconName) == true)
                return "Downloaded";

            if (_preferences?.SelectedIcons?.Contains(iconName) == true)
                return "Selected";

            return "";
        }

        /// <summary>
        /// Update selected icons
        /// </summary>
        public static void UpdateSelectedIcons(List<string> selectedIcons)
        {
            _preferences.SelectedIcons = selectedIcons.ToList();
            SavePreferences();
        }

        /// <summary>
        /// Download selected icons
        /// </summary>
        public static async Task<DownloadResult> DownloadSelectedIconsAsync()
        {
            var result = new DownloadResult();
            var toDownload = _preferences.SelectedIcons
                .Where(icon => !_preferences.DownloadedIcons.Contains(icon))
                .ToList();

            foreach (var iconName in toDownload)
            {
                try
                {
                    var success = await DownloadIconAsync(iconName);
                    if (success)
                    {
                        _preferences.DownloadedIcons.Add(iconName);
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailedIcons.Add(iconName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error downloading {iconName}: {ex.Message}");
                    result.FailedIcons.Add(iconName);
                }
            }

            SavePreferences();
            return result;
        }

        private static async Task<bool> DownloadIconAsync(string iconName)
        {
            try
            {
                var iconInfo = _catalog.Icons.FirstOrDefault(i => i.Name == iconName);
                if (iconInfo == null) return false;

                var svgContent = await _httpClient.GetStringAsync(iconInfo.DownloadUrl);

                // Convert SVG to PNG using SkiaSharp
                var pngBytes = ConvertSvgToPng(svgContent, 18, 18);
                var pngPath = Path.Combine(_downloadedIconsPath, $"{iconName}.png");
                File.WriteAllBytes(pngPath, pngBytes);

                System.Diagnostics.Debug.WriteLine($"Downloaded and converted icon: {iconName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to download {iconName}: {ex.Message}");
                return false;
            }
        }

        private static byte[] ConvertSvgToPng(string svgContent, int width, int height)
        {
            var svg = new SkiaSharp.Extended.Svg.SKSvg();
            svg.FromSvgString(svgContent);

            using (var bitmap = new SkiaSharp.SKBitmap(width, height))
            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                canvas.Clear(SkiaSharp.SKColors.Transparent);
                if (svg.Picture != null)
                {
                    canvas.DrawPicture(svg.Picture);
                }
                using (var image = SkiaSharp.SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        /// <summary>
        /// Get download statistics
        /// </summary>
        public static IconStats GetStats()
        {
            return new IconStats
            {
                TotalAvailable = _catalog?.Icons?.Count ?? 0,
                DefaultIcons = IconCatalog.BuiltInIcons.Count,
                SelectedIcons = _preferences?.SelectedIcons?.Count ?? 0,
                DownloadedIcons = _preferences?.DownloadedIcons?.Count ?? 0,
                StorageUsed = GetStorageUsed()
            };
        }

        /// <summary>
        /// Calculate storage used by downloaded icons
        /// </summary>
        private static long GetStorageUsed()
        {
            try
            {
                if (!Directory.Exists(_downloadedIconsPath)) return 0;

                return Directory.GetFiles(_downloadedIconsPath)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }
    }

    #region Data Models

    public class PhosphorCatalog
    {
        public string Version { get; set; }
        public DateTime LastUpdated { get; set; }
        public int TotalIcons { get; set; }
        public List<string> DefaultIcons { get; set; } = new List<string>();
        public List<PhosphorIconInfo> Icons { get; set; } = new List<PhosphorIconInfo>();
    }

    public class PhosphorIconInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string DownloadUrl { get; set; }
        public bool IsDefault { get; set; }
        public int Order { get; set; }
    }

    public class IconPreferences
    {
        public string CatalogVersion { get; set; } = "2.1.1";
        public List<string> SelectedIcons { get; set; } = new List<string>();
        public List<string> DownloadedIcons { get; set; } = new List<string>();
    }

    public class IconManagementItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDownloaded { get; set; }
        public string Status { get; set; }
    }

    public class DownloadResult
    {
        public int SuccessCount { get; set; }
        public List<string> FailedIcons { get; set; } = new List<string>();
    }

    public class IconStats
    {
        public int TotalAvailable { get; set; }
        public int DefaultIcons { get; set; }
        public int SelectedIcons { get; set; }
        public int DownloadedIcons { get; set; }
        public long StorageUsed { get; set; }
    }

    #endregion

    #region Extensions

    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }
    }

    #endregion
}