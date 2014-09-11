using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    public interface ICustomer : IPayload
    {
        string Firstname { get; set; }

        string Lastname { get; set; }

        void UpdateName(string name);
    }

    [Serializable]
    public class Customer : ICustomer
    {
        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public void UpdateName(string name)
        {
            this.Lastname = name;
        }

    }
}
