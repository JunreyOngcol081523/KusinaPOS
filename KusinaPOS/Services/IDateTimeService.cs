namespace KusinaPOS.Services
{
    public interface IDateTimeService
    {
        string CurrentDateTime { get; }
        event EventHandler<string> DateTimeChanged;
    }
}