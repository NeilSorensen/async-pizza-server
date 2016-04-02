using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DeliveryBoy
{
    class Program
    {
        static void Main(string[] args)
        {
            var serializer = new JsonByteArraySerializer();
            var connectionFactory = new ConnectionFactory();
            connectionFactory.HostName = "localhost";
            connectionFactory.UserName = "guest";
            connectionFactory.VirtualHost = "/";
            connectionFactory.Port = 5672;
            
            
            var oven = new PizzaOven(connectionFactory.CreateConnection().CreateModel(), serializer, new Random());
            oven.Register();

            Console.ReadLine();

            oven.Deregister();
        }
    }
}
