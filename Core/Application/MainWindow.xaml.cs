using System;
using System.Windows;
using gtt_sidebar.Widgets.ClockWidget;
using gtt_sidebar.Widgets.StockWidget;
using gtt_sidebar.Widgets.WeatherWidget;
using gtt_sidebar.Core.Managers;
using gtt_sidebar.Core.Settings;

namespace gtt_sidebar.Core.Application
{
    public partial class MainWindow : Window
    {
        private WidgetManager _widgetManager;
        private SettingsData _currentSettings;
        private SettingsWindow _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();

            // Load settings and apply to window
            _currentSettings = SettingsStorage.LoadSettings();
            ApplySettings(_currentSettings);

            _widgetManager = new WidgetManager();
            LoadWidgets();
        }

        private void ApplySettings(SettingsData settings)
        {
            // Update window dimensions
            this.Width = settings.Window.Width;

            // Apply positioning using WindowPositioner with new settings
            WindowPositioner.PositionSidebarWindow(this, settings.Window);

            System.Diagnostics.Debug.WriteLine($"Applied settings: Position={settings.Window.Position}, Width={settings.Window.Width}");
        }

        private async void LoadWidgets()
        {
            WidgetContainer.Children.Clear();

            // Use the widget manager to discover and load widgets
            var widgets = _widgetManager.DiscoverAndLoadWidgets();

            foreach (var widget in widgets)
            {
                WidgetContainer.Children.Add(widget.GetControl());
            }

            // Initialize all widgets asynchronously
            await _widgetManager.InitializeWidgetsAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Prevent multiple settings windows
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            // Create new settings window
            _settingsWindow = new SettingsWindow(_currentSettings);
            _settingsWindow.SettingsApplied += OnSettingsApplied;

            // Position relative to main window
            _settingsWindow.Left = this.Left - _settingsWindow.Width - 10;
            _settingsWindow.Top = this.Top;

            // Ensure window stays on screen
            if (_settingsWindow.Left < 0)
            {
                _settingsWindow.Left = this.Left + this.Width + 10;
            }

            _settingsWindow.Show();
        }

        private void OnSettingsApplied(SettingsData newSettings)
        {
            _currentSettings = newSettings;

            // Save settings
            SettingsStorage.SaveSettings(_currentSettings);

            // Apply new settings to window
            ApplySettings(_currentSettings);

            System.Diagnostics.Debug.WriteLine("Settings applied and saved");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ResizeMode = ResizeMode.NoResize;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _widgetManager?.DisposeWidgets();

            // Close settings window if open
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Close();
            }
        }
    }
}