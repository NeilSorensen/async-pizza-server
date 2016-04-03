using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryBoy
{
    public class OrderPlaced
    {
        public IList<Pizza> Pizzas { get; set; }
        public string Address { get; set; }
        public string Customer { get; set; }
    }

    public class OrderAccepted
    {
        public Guid OrderId { get; set; }
        public Guid[] Pizzas { get; set; }
        public decimal OrderPrice { get; set; }
        public string Address { get; set; }
        public string Customer { get; set; }
    }

    public class OrderFilled
    {
        public string Address { get; set; }
        public string DeliverTo { get; set; }
        public decimal OrderPrice { get; set; }
        public string CouponCode { get; set; }
        public IList<Pizza> Pizzas { get; set; }
    }

    public class Pizza
    {
        public string[] Toppings { get; set; }
        public PizzaSize Size { get; set; }
    }
}
