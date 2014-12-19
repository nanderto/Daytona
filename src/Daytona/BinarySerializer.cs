namespace Daytona
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;

    [Serializable]
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


        public object Deserializer(string input, Type type)
        {
            throw new NotImplementedException();
        }
    }
}