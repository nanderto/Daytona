using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona.Store
{
    public class DBPayload<T> : IPayload
    {
        T payload = default(T);
        public int Id { get; set; }
        public void AddPayload(T payload) 
        {
            this.payload = payload;
        }

    }
}
