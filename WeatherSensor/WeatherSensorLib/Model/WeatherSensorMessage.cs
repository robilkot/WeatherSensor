namespace WeatherSensorLib.Model
{
    public record WeatherSensorMessage(
        string SensorName,
        DateTimeOffset Timestamp,
        float WindSpeedMean, 
        float WindDirectionMean
        );
}
