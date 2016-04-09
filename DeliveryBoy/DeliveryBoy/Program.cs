using System;
using System.Configuration;
using PizzaHouse.Shared;
using RabbitMQ.Client;

namespace DeliveryBoy
{
    class Program
    {
        static void Main(string[] args)
        {
            var serializer = new JsonByteArraySerializer();
            var connectionFactory = new ConnectionFactory();
            connectionFactory.HostName = ConfigurationManager.AppSettings["RabbitHost"];
            connectionFactory.UserName = ConfigurationManager.AppSettings["RabbitUsername"];
            connectionFactory.Password = ConfigurationManager.AppSettings["RabbitPassword"];
            connectionFactory.VirtualHost = ConfigurationManager.AppSettings["RabbitVirtualHost"];
            connectionFactory.Port = int.Parse(ConfigurationManager.AppSettings["RabbitPort"]); 
            
            var register = new CashRegister(connectionFactory.CreateConnection().CreateModel(), serializer, new CouponVerifier());
            var oven = new PizzaOven(connectionFactory.CreateConnection().CreateModel(), serializer, new Random());
            var orderAssembler = new OrderAssembler(connectionFactory.CreateConnection().CreateModel(), serializer, new CouponVerifier());
            register.Register();
            oven.Register();
            orderAssembler.Register();

            Console.ReadLine();
            
            oven.Deregister();
            register.Deregister();
            orderAssembler.Deregister();
        }
    }
}
