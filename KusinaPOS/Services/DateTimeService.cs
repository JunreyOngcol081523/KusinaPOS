namespace KusinaPOS.Services
{
    public class DateTimeService : IDateTimeService, IDisposable
    {
        private Timer? _timer;
        private string _currentDateTime = string.Empty;
        private bool _disposed;

        public string CurrentDateTime => _currentDateTime;
        public event EventHandler<string>? DateTimeChanged;

        public DateTimeService()
        {
            // Initialize timer but don’t start until service is fully created
            _timer = new Timer(_ => UpdateDateTimeSafe(), null, 0, 1000);
        }

        private void UpdateDateTimeSafe()
        {
            if (_disposed) return; // stop if service disposed

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy | h:mm tt");
                    DateTimeChanged?.Invoke(this, _currentDateTime);
                });
            }
            catch (InvalidOperationException)
            {
                // Main thread not available (app closing), ignore
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer?.Dispose();
            _timer = null;
        }
    }
}
