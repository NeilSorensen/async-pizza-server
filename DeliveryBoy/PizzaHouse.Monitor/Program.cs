using System;
using System.Configuration;
using PizzaHouse.Shared;
using RabbitMQ.Client;

namespace PizzaHouse.Monitor
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

            var monitor = new OrderMonitor(connectionFactory.CreateConnection().CreateModel(), serializer, new CouponVerifier());
            monitor.RegisterListener();

            Console.ReadLine();
            monitor.CancelListener();
        }
    }
}
