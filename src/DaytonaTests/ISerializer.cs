using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaytonaTests
{
    interface ISerializer
    {
        byte[] GetBuffer<T>(Encoding encoding, T message);
        T Deserializer<T>(string input);
    }
}
