using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    public class Customer : ICustomer, IPayload
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }

        public void UpdateName(string name)
        {
            this.Firstname = name;
        }
    }

    public interface ICustomer
    {
        void UpdateName(string name);
    }
}
