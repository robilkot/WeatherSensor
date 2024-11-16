using System.Text;
using System.Text.Json;
using WeatherSensorLib.Messages;

namespace WeatherSensorLib.Services
{
    public class PersistenceService<T> where T : ISensorMessage
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            WriteIndented = false,
        };

        public (bool, Exception?) TryAppendToFile(T message, string filename)
        {
            try
            {
                using var reader = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                int offset = 2;
                byte[] lastSymbols = new byte[offset];

                // Making sure we don't look beyond the stream size
                reader.Seek(-Math.Min(reader.Length, offset), SeekOrigin.End);
                reader.Read(lastSymbols, 0, offset);

                // If file already contains a list of messages
                if (lastSymbols is [.., (byte)']'])
                {
                    // Move stream to last record in list
                    reader.Seek(-offset, SeekOrigin.End);
                    bool fileAlreadyContainsMessages = reader.ReadByte() == '}';

                    var serializedMessage = JsonSerializer.Serialize(message, s_options);
                    
                    // Append comma
                    if (fileAlreadyContainsMessages)
                    {
                        reader.Write([(byte)',']);
                    }

                    reader.Write(Encoding.UTF8.GetBytes(serializedMessage));

                    // Append list enclosure because it's been overwritten by message
                    reader.Write([(byte)']']);
                }
                // If the file is yet empty
                else
                {
                    var serializedMessage = JsonSerializer.Serialize(new List<T>() { message }, s_options);

                    reader.Write(Encoding.UTF8.GetBytes(serializedMessage));
                }
            }
            catch (Exception e)
            {
                return (false, e);
            }

            return (true, null);
        }
    }
}
