using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaytonaTests
{
    public class Customer : IPayload
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }

    public interface IAccount
    {
        void UpdateHolder(string name);

        void ClearHolder();
    }

    public class Account : IAccount
    {
        public int Number { get; set; }

        public string Holder { get; set; }

        public void UpdateHolder(string name)
        {
            this.Holder = name;
        }

        public void ClearHolder()
        {
            this.Holder = string.Empty;
        }
    }
}
