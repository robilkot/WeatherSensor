
using PresentApp.Services;
using WeatherSensoeConsoleApp;
using WeatherSensorLib.Model;

CommunicationService svc = new();

Console.WriteLine("Available ports:");
svc.AvailablePortsNames.Each((port, index) =>
{
    Console.WriteLine($"{index}: {port}");
});

var portIndex = int.Parse(Console.ReadLine()!);

ConnectionParameters parameters = new(svc.AvailablePortsNames[portIndex]);

svc.MessageReceived += (sender, message) =>
{
    Console.WriteLine($"{message.Timestamp} {message.WindSpeedMean} {message.WindDirectionMean}");
};

svc.MessageCorrupted += (sender, args) =>
{
    Console.WriteLine("Message corrupted");
};

svc.TryConnect(parameters);

Console.ReadKey();