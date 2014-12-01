using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiloConsole
{
    public class RunningActors
    {
        public string Address { get; set; }

        public RunningActors(string address)
        {
            this.Address = address;
        }
    }
}
