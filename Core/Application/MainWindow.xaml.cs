using System;
using System.Windows;
using gtt_sidebar.Widgets.ClockWidget;
using gtt_sidebar.Widgets.StockWidget;
using gtt_sidebar.Widgets.WeatherWidget;
using gtt_sidebar.Core.Managers;

namespace gtt_sidebar.Core.Application
{
    public partial class MainWindow : Window
    {
        private WidgetManager _widgetManager;

        public MainWindow()
        {
            InitializeComponent();
            WindowPositioner.PositionSidebarWindow(this);
            _widgetManager = new WidgetManager();
            LoadWidgets();
        }

        private async void LoadWidgets()
        {
            WidgetContainer.Children.Clear();

            // Use the new widget manager to discover and load widgets
            var widgets = _widgetManager.DiscoverAndLoadWidgets();
            
            foreach (var widget in widgets)
            {
                WidgetContainer.Children.Add(widget.GetControl());
            }
            
            // Initialize all widgets asynchronously
            await _widgetManager.InitializeWidgetsAsync();
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
        }
    }
}