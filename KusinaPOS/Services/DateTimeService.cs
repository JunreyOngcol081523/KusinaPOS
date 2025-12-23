namespace KusinaPOS.Services
{
    public class DateTimeService : IDateTimeService
    {
        private Timer? _timer;
        private string _currentDateTime = string.Empty;

        public string CurrentDateTime => _currentDateTime;
        public event EventHandler<string>? DateTimeChanged;

        public DateTimeService()
        {
            UpdateDateTime();
            _timer = new Timer(_ => UpdateDateTime(), null, 0, 1000);
        }

        private void UpdateDateTime()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy | h:mm tt");
                DateTimeChanged?.Invoke(this, _currentDateTime);
            });
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}