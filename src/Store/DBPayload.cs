using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daytona.Store
{
    [Serializable]
    public class DBPayload<T> : IPayload, IPayload<T>
    {
        public int Id { get; set; }
        
        public T Payload { get; set; }
    }
}
