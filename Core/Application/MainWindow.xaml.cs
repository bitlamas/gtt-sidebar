using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
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
        private NotifyIcon _trayIcon;
        private bool _isClosing = false;

        public MainWindow()
        {
            InitializeComponent();

            // Load settings and apply to window
            _currentSettings = SettingsStorage.LoadSettings();
            ApplySettings(_currentSettings);

            _widgetManager = new WidgetManager();
            LoadWidgets();
            CreateTrayIcon();
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            try
            {
                // Load icon from embedded resource
                var iconUri = new Uri("pack://application:,,,/gtt-sidebar-icon.ico");
                var resourceStream = System.Windows.Application.GetResourceStream(iconUri);
                if (resourceStream != null)
                {
                    _trayIcon.Icon = new System.Drawing.Icon(resourceStream.Stream);
                }
                else
                {
                    _trayIcon.Icon = SystemIcons.Application;
                }
            }
            catch (Exception)
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
            _trayIcon.Text = "gtt sidebar";
            _trayIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Hide", null, (s, e) => HideWindow());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Settings", null, (s, e) => SettingsButton_Click(null, null));
            var alwaysOnTopItem = contextMenu.Items.Add("Always on Top", null, (s, e) => ToggleAlwaysOnTop());
            ((ToolStripMenuItem)alwaysOnTopItem).Checked = this.Topmost; // Use window property
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += (s, e) => ToggleWindow();
        }

        private void ToggleAlwaysOnTop()
        {
            this.Topmost = !this.Topmost;
            UpdateTrayMenuCheck();
        }

       

        private void UpdateTrayMenuCheck()
        {
            for (int i = 0; i < _trayIcon.ContextMenuStrip.Items.Count; i++)
            {
                if (_trayIcon.ContextMenuStrip.Items[i].Text == "Always on Top")
                {
                    ((ToolStripMenuItem)_trayIcon.ContextMenuStrip.Items[i]).Checked = this.Topmost;
                    break;
                }
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void HideWindow()
        {
            this.Hide();
        }

        private void ToggleWindow()
        {
            if (this.IsVisible)
                HideWindow();
            else
                ShowWindow();
        }

        private void ExitApplication()
        {
            _isClosing = true;
            System.Windows.Application.Current.Shutdown();
        }

        private void ApplySettings(SettingsData settings)
        {
            this.Width = settings.Window.Width;
            WindowPositioner.PositionSidebarWindow(this, settings.Window);
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
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            // Reload current settings before opening window
            _currentSettings = SettingsStorage.LoadSettings();

            _settingsWindow = new SettingsWindow(_currentSettings);
            _settingsWindow.SettingsApplied += OnSettingsApplied;

            _settingsWindow.Left = this.Left - _settingsWindow.Width - 10;
            _settingsWindow.Top = this.Top;

            if (_settingsWindow.Left < 0)
            {
                _settingsWindow.Left = this.Left + this.Width + 10;
            }

            _settingsWindow.Show();
        }

        private void OnSettingsApplied(SettingsData newSettings)
        {
            _currentSettings = newSettings;
            SettingsStorage.SaveSettings(_currentSettings);
            ApplySettings(_currentSettings);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ResizeMode = ResizeMode.NoResize;
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnClosed(e);
            _widgetManager?.DisposeWidgets();

            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                HideWindow();
            }
            base.OnClosing(e);
        }
    }
}