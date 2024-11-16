
using PresentApp.Services;
using WeatherSensoeConsoleApp;
using WeatherSensorLib.Model;
using WeatherSensorLib.Services;

PersistenceService persistenceService = new();
CommunicationService svc = new();

Console.WriteLine("Available ports:");
svc.AvailablePortsNames.Each((port, index) =>
{
    Console.WriteLine($"{index}: {port}");
});


Console.WriteLine("Input port index:");
var portIndex = int.Parse(Console.ReadLine()!);

ConnectionParameters parameters = new(svc.AvailablePortsNames[portIndex]);

svc.MessageReceived += (sender, message) =>
{
    Console.WriteLine($"{message.Timestamp} {message.WindSpeedMean} {message.WindDirectionMean}");

    if (persistenceService.TryAppendToFile(message, "output.json") is (false, Exception ex))
    {
        Console.WriteLine($"Exception while saving message: {ex.Message}");
    }
};

svc.MessageCorrupted += (sender, args) =>
{
    Console.WriteLine("Message corrupted");
};

svc.TryConnect(parameters);

Console.ReadKey();