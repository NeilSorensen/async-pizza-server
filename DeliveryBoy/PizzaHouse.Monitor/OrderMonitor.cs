using System;
using System.Text;
using PizzaHouse.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PizzaHouse.Monitor
{
    public class OrderMonitor
    {
        private readonly IModel model;
        private readonly JsonByteArraySerializer serializer;
        private readonly CouponVerifier verifier;
        private EventingBasicConsumer consumer;
        private string consumerTag;
        private const string QueueName = ExchangeName + "=>order_monitor";
        private const string ExchangeName = "order_placed";
        public OrderMonitor(IModel model, JsonByteArraySerializer serializer, CouponVerifier verifier)
        {
            this.model = model;
            this.serializer = serializer;
            this.verifier = verifier;
        }

        public void RegisterListener()
        {
            model.QueueDeclare(QueueName, true, false, false, null);
            model.QueueBind(QueueName, ExchangeName, "");
            model.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, true);

            consumer = new EventingBasicConsumer(model);
            consumer.Received += OrderRecieved;
            consumerTag = model.BasicConsume(QueueName, false, consumer);
        }

        private void OrderRecieved(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var order = serializer.Deserialize<SimpleOrder>(e.Body);
                CheckOrderMessage(order);
            }
            catch (Exception)
            {
                Console.WriteLine("Unprocessable Message! " + Encoding.UTF8.GetString(e.Body));
            }
            model.BasicAck(e.DeliveryTag, false);
        }

        private void CheckOrderMessage(SimpleOrder order)
        {
            try
            {
                Console.WriteLine(order.Customer + " has placed an order for pizza");
                if (!string.IsNullOrWhiteSpace(order.Coupon))
                {
                    if (verifier.CouponIsValid(order.Coupon, order.Customer))
                    {
                        Console.WriteLine("Congratulations! " + order.Customer + " has successfully redeemed a coupon!");
                    }
                    else
                    {
                        Console.WriteLine(order.Customer + " has attempted to redeem an invalid coupon!");
                    }
                }
            }
            catch (Exception) { }
        }

        public void CancelListener()
        {
            model.BasicCancel(consumerTag);
        }
    }

    public class SimpleOrder
    {
        public string Customer { get; set; }
        public string Coupon { get; set; }
    }
}
