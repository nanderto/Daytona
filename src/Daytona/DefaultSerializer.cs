namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class BinarySerializer : ISerializer
    {
        public Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }

        public T Deserializer<T>(byte[] input)
        {
            var memoryStream = new MemoryStream(input);
            var binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        public object Deserializer(byte[] input, Type type)
        {
            var memoryStream = new MemoryStream(input);
            var binaryFormatter = new BinaryFormatter();
            return binaryFormatter.Deserialize(memoryStream);
        }

        public T Deserializer<T>(string input)
        {
            throw new NotImplementedException();
        }

        public byte[] GetBuffer<T>(T message)
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, message);
            memoryStream.Close();
            return memoryStream.GetBuffer();
        }

        public string GetString(byte[] buffer)
        {
            var memoryStream = new MemoryStream(buffer);
            var binaryFormatter = new BinaryFormatter();
            return (string) binaryFormatter.Deserialize(memoryStream);
        }
    }

    public class DefaultSerializer : ISerializer
    {
        Encoding encoding;

        public DefaultSerializer(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public byte[] GetBuffer<T>(T message)
        {
            return encoding.GetBytes(this.Serialize<T>(message));
        }

        private string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None);
        }

        public T Deserializer<T>(byte[] input)
        {
            return JsonConvert.DeserializeObject<T>(encoding.GetString(input));
        }

        public object Deserializer(byte[] input, Type type)
        {
            return JsonConvert.DeserializeObject(encoding.GetString(input), type);
        }

        public string GetString(byte[] buffer)
        {
            return encoding.GetString(buffer);
        }

        public T Deserializer<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input);
        }

        //public bool TryDeserializer<T>(byte[] inputBuffer, out T result)
        //{
        //    try
        //    {
        //        result = Deserializer<T>(inputBuffer);
        //    }
        //    catch
        //    {
        //        result = default(T);
        //        return false;
        //    }
        //    return false;
        //}

        public Encoding Encoding
        {
            get { return this.encoding; }
        }
    }
}
