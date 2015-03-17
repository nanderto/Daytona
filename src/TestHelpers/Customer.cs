using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestHelpers
{
    using Newtonsoft.Json;

    public interface ICustomer : IPayload
    {
        long Id { set; }

        string Firstname { get; set; }

        string Lastname { get; set; }

        void UpdateName(string name);

        void CreateOrder();
    }

    [Serializable]
    public class Customer : ActorFactory, ICustomer 
    {
        private readonly List<Guid> orders = new List<Guid>();

        public List<Guid> Orders
        {
            get
            {            
                return this.orders;
            }
        }
 
        public Customer(long id)
        {
            this.Id = id;
        }

        public Customer(Actor factory)
            : base(factory)
        {
        }

        public Customer()
        {
            
        }

        public long Id { private get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        [JsonIgnore]
        public override Actor Factory { get; set; }

        public void UpdateName(string name)
        {
            this.Lastname = name;
        }

        public void CreateOrder()
        {
            var newId = Guid.NewGuid();
            var order = this.Factory.CreateInstance<Order>(typeof(Order), newId);          
            order.CreateOrder("this is totally my description", 3, Guid.NewGuid().ToString().Replace("-", string.Empty), this.Id);
            this.Orders.Add(newId);
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

        int Quantity { get; set; }

        string StringID { get; set; }

        long CustomerID { get; set; }
       
        string ProductID { get; set; }

        Guid Id { get; set; }

        void CreateOrder(string description, int quantity, string productId, long customerId);

        void UpdateDescription(string description);
        
        void UpdateOrder(Guid uniqueGuid, string description, int quantity, string productId, long customerId);
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

        public void CreateOrder(string description, int quantity, string productId, long customerId)
        {
            this.Description = description;
            this.Quantity = quantity;
            this.ProductID = productId;
            this.CustomerID = customerId;
        }

        public void UpdateDescription(string description)
        {
            this.Description = description;
        }

        public string Description { get; set; }

        public int Quantity { get; set; }

        public string ProductID { get; set; }



        public string StringID { get; set; }

        public Guid Id { get; set; }

        public long CustomerID { get; set; }


        public void UpdateOrder(Guid uniqueGuid, string description, int quantity, string productId, long customerId)
        {
            this.Description = description;
            this.Quantity = quantity;
            this.ProductID = productId;
            this.CustomerID = customerId;
        }
    }
}
