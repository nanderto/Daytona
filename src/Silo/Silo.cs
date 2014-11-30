using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    using ZeroMQ;

    public class Silo : Actor
    {
        public Silo(ZmqContext context, ISerializer serializer, string inRoute) : base(context, serializer, inRoute)
        {
            
        }
    }
}
