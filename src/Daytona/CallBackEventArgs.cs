using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public class CallBackEventArgs : EventArgs
    {
        public Exception Error { get; set; }
        public bool Cancelled { get; set; }
        public int Result { get; set; }
        public List<IPayload> Payload { get; set; }
    }
}
