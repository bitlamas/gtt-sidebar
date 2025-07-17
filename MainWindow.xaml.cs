using System;
using System.Windows;
using gtt_sidebar.Widgets;

namespace gtt_sidebar
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PositionWindow();
            LoadWidgets();
        }

        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height;

            var windowWidth = 127;
            var marginRight = 3;
            var marginTop = 5;
            var marginBottom = 5;

            this.Width = windowWidth;
            this.Height = screenHeight - taskbarHeight - marginTop - marginBottom;
            this.Left = screenWidth - windowWidth - marginRight;
            this.Top = marginTop;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        private async void LoadWidgets()
        {
            WidgetContainer.Children.Clear();

            // Add clock widget
            var clockWidget = new ClockWidget();
            await clockWidget.InitializeAsync();
            WidgetContainer.Children.Add(clockWidget);

            // Add weather widget
            var weatherWidget = new WeatherWidget();
            await weatherWidget.InitializeAsync();
            WidgetContainer.Children.Add(weatherWidget);

            // Add stock widget
            var stockWidget = new StockWidget();
            await stockWidget.InitializeAsync();
            WidgetContainer.Children.Add(stockWidget);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.ResizeMode = ResizeMode.NoResize;
        }
    }
}