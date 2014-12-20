using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    public interface ICustomer : IPayload
    {
        long Id { get; set; }

        string Firstname { get; set; }

        string Lastname { get; set; }

        void UpdateName(string name);
    }

    [Serializable]
    public class Customer : ICustomer
    {
        private readonly long id;

        public long Id { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public void UpdateName(string name)
        {
            this.Lastname = name;
        }

        public Customer(long id)
        {
            this.id = id;
        }

        public Customer()
        {
            
        }
        //public Customer(string id)
        //{
        //    long longId = 0;
        //    long.TryParse(id, out longId);
        //    this.id = longId;
        //}
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
            this.Id = Guid.NewGuid();
        }

        public Order(Guid id)
        {
            this.Id = id;
        }

        public void UpdateDescription(string description)
        {
            this.Description = description;
        }

        public string Description { get; set; }

        public Guid Id { get; private set; }
    }
}
