using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaytonaTests
{
    public class Serializer : ISerializer
    {
        public byte[] GetBuffer<T>(Encoding encoding, T message)
        {
            return encoding.GetBytes(this.Serialize<T>(message));
        }

        private string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None);
        }


        public T Deserializer<T>(string input)
        {
            throw new NotImplementedException();
        }
    }
}
