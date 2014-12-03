using Daytona;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public class DefaultSerializer : ISerializer
    {
        Encoding encoding;

        public DefaultSerializer(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public byte[] GetBuffer<T>(T message)
        {
            return this.encoding.GetBytes(this.Serialize<T>(message));
        }

        public string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None);
        }

        public byte[] GetBufferizedPayload<T>(IPayload<T> message)
        {
            return encoding.GetBytes(this.GetSerializedPayload<T>(message));
        }

        public byte[] GetBufferizedPayload<T>(DtoPayload<T> message)
        {
            return encoding.GetBytes(this.GetSerializedPayload<T>(message));
        }

        public string GetSerializedPayload<T>(DtoPayload<T> message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None);
        }

        public string GetSerializedPayload<T>(IPayload<T> message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None);
        }

        public T Deserializer<T>(byte[] input)
        {
            return JsonConvert.DeserializeObject<T>(encoding.GetString(input));
        }

        public static DtoPayload<T> DeserializeDBPayload<T>(string body)
        {
            DtoPayload<T> pl = (DtoPayload<T>)JsonConvert.DeserializeObject<DtoPayload<T>>(body, new DtoPayloadConverter());
            return pl;
        }

        public string GetString(byte[] buffer)
        {
            return encoding.GetString(buffer);
        }

        public T Deserializer<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input);
        }

        public Encoding Encoding
        {
            get { return this.encoding; }
        }
    }
}
