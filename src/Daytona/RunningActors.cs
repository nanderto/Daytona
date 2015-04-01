using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public class RunningActors
    {
        public DateTime LastHeartbeat { get; set; }

        public string Address { get; set; }

        public RunningActors(string address)
        {
            LastHeartbeat = DateTime.UtcNow;
            this.Address = address;
        }
    }
}
