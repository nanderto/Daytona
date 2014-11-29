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

    public interface IOrder
    {
        string Description { get; set; }

        Guid Id { get; }

        void UpdateDescription(string description);
    }

    [Serializable]
    public class Order : IOrder
    {
        public Order()
        {
            this.Id = new Guid();
        }

        public void UpdateDescription(string description)
        {
            this.Description = description;
        }
        public string Description { get; set; }

        public Guid Id { get; private set; }
    }
}
