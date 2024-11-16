using System.IO.Ports;
using System.Management;
using WeatherSensorLib.Model;
using WeatherSensorLib.Services;

namespace PresentApp.Services
{
    public partial class CommunicationService
    {
        public event EventHandler<WeatherSensorMessage>? MessageReceived = null;
        public event EventHandler? MessageCorrupted = null;

        // List of names implemented for convenience in future use in UI
        public string[] AvailablePortsNames { get; private set; } = [];

        private WeatherSensorReader _reader = default!;
        private SerialPort? _activePort = null;

        public CommunicationService()
        {
            OnDeviceConfigurationChanged();
            StartDevicesEventsWatcher();
        }

        public (bool, Exception?) TryConnect(ConnectionParameters parameters)
        {
            SerialPort port = new()
            {
                PortName = parameters.PortName,
                BaudRate = parameters.BaudRate,
                DataBits = parameters.DataBits,
                StopBits = parameters.StopBits switch
                {
                    1 => StopBits.One,
                    1.5f => StopBits.OnePointFive,
                    2 => StopBits.Two,
                    _ => throw new NotImplementedException()
                },
            };

            try
            {
                port.Open();
            }
            catch (Exception ex)
            {
                return (false, ex);
            }

            _activePort = port;
            _activePort.DataReceived += OnReceiveData;

            _reader = new(parameters.PortName);
            _reader.MessageReceived += (sender, message) => MessageReceived?.Invoke(sender, message);
            _reader.MessageCorrupted += (sender, message) => MessageCorrupted?.Invoke(sender, EventArgs.Empty);

            return (true, null);
        }

        public (bool, IOException? ex) TryDisconnect()
        {
            if (_activePort is not null)
            {
                if (_activePort.IsOpen)
                {
                    try
                    {
                        _activePort.Close();
                    }
                    catch (IOException ex)
                    {
                        return (false, ex);
                    }
                    _activePort.Dispose();
                }

                _activePort.DataReceived -= OnReceiveData;
                _activePort = null;
            }

            return (true, null);
        }

        // Starts listening for device configuration changed events
        // https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-devicechangeevent
        private void StartDevicesEventsWatcher()
        {
#pragma warning disable CA1416 // Currently only running on windows
            var watcher = new ManagementEventWatcher();
            watcher.EventArrived += new EventArrivedEventHandler((object sender, EventArrivedEventArgs e) => OnDeviceConfigurationChanged());
            watcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 1");
            watcher.Start();
#pragma warning restore CA1416
        }

        private void OnDeviceConfigurationChanged()
        {
            if (_activePort is not null)
            {
                // If device was removed externally
                if (!_activePort.IsOpen)
                {
                    _activePort.DataReceived -= OnReceiveData;
                    _activePort = null;
                }
                return;
            }

            AvailablePortsNames = SerialPort.GetPortNames();
        }

        private void OnReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            while (_activePort?.BytesToRead > 0)
            {
                var b = _activePort.ReadByte();

                // End of stream reached
                if (b == -1)
                    return;

                _reader.Next((byte)b);
            }
        }
    }
}
