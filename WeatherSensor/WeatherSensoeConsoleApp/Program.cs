
using PresentApp.Services;
using System.IO.Ports;
using WeatherSensoeConsoleApp;
using WeatherSensorLib.Messages;
using WeatherSensorLib.Readers;
using WeatherSensorLib.Services;


// Choose active port
var ports = SerialPort.GetPortNames();

Console.WriteLine("Available ports:");
ports.Each((port, index) =>
{
    Console.WriteLine($"{index}: {port}");
});

Console.WriteLine("Input port index:");
var portIndex = int.Parse(Console.ReadLine()!);

var parameters = new ConnectionParameters(ports[portIndex]);

// Create needed services
var sensorReader = new WeatherSensorReader(parameters.PortName);
var svc = new CommunicationService<WeatherSensorMessage>(sensorReader);
var persistenceService = new PersistenceService<WeatherSensorMessage>();

// Subscribe to events
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

// Start monitoring
svc.TryConnect(parameters);

Console.ReadKey();

svc.TryDisconnect();