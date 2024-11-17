using WeatherSensorLib.Messages;

namespace WeatherSensorLib.Readers
{
    public class WeatherSensorReader : ISensorReader<WeatherSensorMessage>
    {
        abstract record ReaderState
        {
            public sealed record Start : ReaderState;
            public sealed record ReadingField(byte CurrentByte) : ReaderState;
            public sealed record Separator : ReaderState;
            public sealed record CR : ReaderState;
            public sealed record LF : ReaderState;
            public sealed record Corrupted : ReaderState;
        }

        public event EventHandler<WeatherSensorMessage>? MessageReceived;
        public event EventHandler? MessageCorrupted;

        private readonly string _sensorName;
        private float? _windSpeed = null;
        private float? _windDirection = null;

        private MemoryStream _currentField = new(3);
        private ReaderState _state = new ReaderState.Start();

        public WeatherSensorReader(string SensorName)
        {
            _sensorName = SensorName;
        }

        public void Next(byte data)
        {
            _state = GetNextState(_state, data);

            HandleState(_state);
        }

        // Cover syntax according to given protocol
        private static ReaderState GetNextState(ReaderState current, byte data)
            => (char)data switch
            {
                '$' => new ReaderState.Start(),
                ',' when current is ReaderState.ReadingField => new ReaderState.Separator(),
                >= '0' and <= '9' or '.' when current is ReaderState.Start or ReaderState.Separator or ReaderState.ReadingField => new ReaderState.ReadingField(data),
                '\r' when current is ReaderState.ReadingField => new ReaderState.CR(),
                '\n' when current is ReaderState.CR => new ReaderState.LF(),

                _ => new ReaderState.Corrupted(),
            };

        private void HandleState(ReaderState state)
        {
            switch (state)
            {
                case ReaderState.ReadingField read: _currentField.Write([read.CurrentByte], 0, 1); break;
                case ReaderState.Separator:
                case ReaderState.CR: DumpCurrentField(); break;
                case ReaderState.LF: OnMessageCompleted(); break;
                case ReaderState.Corrupted: OnMessageCorrupted(); break;
            }
        }

        private void DumpCurrentField()
        {

            if (!float.TryParse(_currentField.ToArray(), out float field))
            {
                _state = new ReaderState.Corrupted();
                return;
            }

            _currentField.SetLength(0);

            if (_windSpeed is null)
            {
                _windSpeed = field;
                return;
            }

            if (_windDirection is null)
            {
                _windDirection = field;
                return;
            }
        }

        private void OnMessageCorrupted()
        {
            MessageCorrupted?.Invoke(this, EventArgs.Empty);

            ClearCurrentMessageFields();
        }

        private void OnMessageCompleted()
        {
            // Non-nullability is ensured by grammar state machine
            MessageReceived?.Invoke(this, new(_sensorName, DateTimeOffset.Now, _windSpeed!.Value, _windDirection!.Value));

            ClearCurrentMessageFields();
        }

        private void ClearCurrentMessageFields()
        {
            _windDirection = null;
            _windSpeed = null;
        }
    }
}
