namespace WeatherSensorLib.Messages
{
    public record WeatherSensorMessage(
        string SensorName,
        DateTimeOffset Timestamp,
        float WindSpeedMean,
        float WindDirectionMean
        ) : ISensorMessage;
}
