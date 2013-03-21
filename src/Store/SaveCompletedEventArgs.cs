using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona.Store
{
    public class SaveCompletedEventArgs : EventArgs
    {
        public Exception Error { get; set; }
        public bool Cancelled { get; set; }
        public int Result { get; set; }
    }
}
