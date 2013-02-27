using Daytona;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    public class Serializer : ISerializer
    {
        Encoding encoding;

        public Serializer(Encoding encoding)
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

        public string GetString(byte[] buffer)
        {
            return encoding.GetString(buffer);
        }
    }
}
