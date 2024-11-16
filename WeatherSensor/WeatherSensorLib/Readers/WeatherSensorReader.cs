using WeatherSensorLib.Messages;

namespace WeatherSensorLib.Readers
{
    public class WeatherSensorReader : ISensorReader<WeatherSensorMessage>
    {
        record ReaderState
        {
            private ReaderState() { }

            public record Start : ReaderState;
            public record ReadingField(byte CurrentByte) : ReaderState;
            public record Separator : ReaderState;
            public record CR : ReaderState;
            public record LF : ReaderState;
            public record Corrupted(byte CurrentByte) : ReaderState;
        }

        public event EventHandler<WeatherSensorMessage>? MessageReceived;
        public event EventHandler<byte>? MessageCorrupted;

        private readonly string _sensorName;
        private float? _windSpeed = null;
        private float? _windDirection = null;

        private MemoryStream _currentField = new(3);
        private ReaderState _state = new ReaderState.LF();

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
                '$' when current is not ReaderState.Start => new ReaderState.Start(),
                ',' when current is ReaderState.ReadingField => new ReaderState.Separator(),
                >= '0' and <= '9' or '.' when current is ReaderState.Start or ReaderState.Separator or ReaderState.ReadingField => new ReaderState.ReadingField(data),
                '\r' when current is ReaderState.ReadingField => new ReaderState.CR(),
                '\n' when current is ReaderState.CR => new ReaderState.LF(),

                _ => new ReaderState.Corrupted(data),
            };

        private void HandleState(ReaderState state)
        {
            switch (state)
            {
                case ReaderState.ReadingField read: _currentField.Write([read.CurrentByte], 0, 1); break;
                case ReaderState.Separator:
                case ReaderState.CR: DumpCurrentField(); break;
                case ReaderState.LF: OnMessageCompleted(); break;
                case ReaderState.Corrupted corrupted: OnMessageCorrupted(corrupted.CurrentByte); break;
            }
        }

        private void DumpCurrentField()
        {
            var field = float.Parse(_currentField.ToArray());
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

        private void OnMessageCorrupted(byte CurrentByte)
        {
            MessageCorrupted?.Invoke(this, CurrentByte);

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
