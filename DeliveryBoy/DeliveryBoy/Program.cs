using System;
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
            
            var register = new CashRegister(connectionFactory.CreateConnection().CreateModel(), serializer);
            var oven = new PizzaOven(connectionFactory.CreateConnection().CreateModel(), serializer, new Random());
            register.Register();
            oven.Register();

            Console.ReadLine();
            
            oven.Deregister();
            register.Register();
        }
    }
}
