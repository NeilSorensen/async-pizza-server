using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryBoy
{
    public class PizzaOrder
    {
        public string[] Toppings { get; set; }
        public PizzaSize Size { get; set; }
        public Guid OrderId { get; set; }
        public Guid PizzaId { get; set; }
    }

    public enum PizzaSize
    {
        Personal,
        Small,
        Medium,
        Large,
        XLarge
    }

    public class PizzaBaked
    {
        public string[] Toppings { get; set; }
        public PizzaSize Size { get; set; }
        public double Quality { get; set; }
        public Guid OrderId { get; set; }
        public Guid PizzaId { get; set; }
    }
}
