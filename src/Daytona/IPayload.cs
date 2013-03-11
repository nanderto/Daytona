using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public interface IPayload
    {
    }

    public interface IPayload<T>
    {
        T Payload { get; set; }
    }
}
