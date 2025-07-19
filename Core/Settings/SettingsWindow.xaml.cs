using System;
using System.Windows;
using System.Windows.Input;

namespace gtt_sidebar.Core.Settings
{
    public partial class SettingsWindow : Window
    {
        private SettingsData _originalSettings;
        private SettingsData _currentSettings;

        public event Action<SettingsData> SettingsApplied;

        public SettingsWindow(SettingsData currentSettings)
        {
            InitializeComponent();

            _originalSettings = currentSettings;
            _currentSettings = CloneSettings(currentSettings);

            LoadSettingsToUI();
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
                Version = settings.Version
            };
        }

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

            // Apply settings
            SettingsApplied?.Invoke(_currentSettings);
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