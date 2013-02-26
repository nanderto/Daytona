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
    }
}
