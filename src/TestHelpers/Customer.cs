using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    public class Customer : IPayload
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
}
