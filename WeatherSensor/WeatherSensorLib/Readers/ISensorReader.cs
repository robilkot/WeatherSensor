using WeatherSensorLib.Messages;

namespace WeatherSensorLib.Readers
{
    public interface ISensorReader<T> where T : ISensorMessage
    {
        void Next(byte Data);
        event EventHandler<T>? MessageReceived;
        event EventHandler? MessageCorrupted;
    }
}
