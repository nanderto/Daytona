using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    using ZeroMQ;

    public class Silo
    {
        //public Silo(ZmqContext context, ISerializer serializer, string inRoute) : base(context, serializer, inRoute)
        //{
            
        //}

        public Silo(Actor starter)
        {
            this.Starter = starter;
        }


        public Actor Starter { get; set; }
    }
}
