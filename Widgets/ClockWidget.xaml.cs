using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using gtt_sidebar.Interfaces;

namespace gtt_sidebar.Widgets
{
    public partial class ClockWidget : UserControl, IWidget
    {
        private DispatcherTimer _timer;

        public string Name => "Clock";

        public ClockWidget()
        {
            InitializeComponent();
        }

        public UserControl GetControl() => this;

        public Task InitializeAsync()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateDisplay(); // Update immediately
            return Task.CompletedTask;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now;
            TimeDisplay.Text = now.ToString("HH:mm:ss");
            DateDisplay.Text = now.ToString("MMMM dd, yyyy");
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer = null;
        }
    }
}