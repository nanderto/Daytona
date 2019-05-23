using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona
{
    public class Sender
    {
        public string ReturnedAddress { get; set; }

        public Sender(string returnedAddress)
        {
            this.ReturnedAddress = returnedAddress;
        }
    }
}
