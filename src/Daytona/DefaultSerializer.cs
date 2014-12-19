namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

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

        public object Deserializer(string input, Type type)
        {
            return JsonConvert.DeserializeObject(input, type);
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
