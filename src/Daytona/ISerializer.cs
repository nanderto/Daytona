using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public interface ISerializer
    {
        byte[] GetBuffer<T>(T message);

        T Deserializer<T>(byte[] input);

        string GetString(byte[] buffer);

        //bool TryDeserializer<T>(byte[] inputBuffer, out T result);

        T Deserializer<T>(string input);
    }
}
