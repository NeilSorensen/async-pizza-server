using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliveryBoy
{
    class PizzaOven
    {
        private const string QueueName = "pizza_ordered=>pizza_oven";
        public const string PizzaBakedExchange = "pizza_baked";

        private readonly IModel model;
        private readonly JsonByteArraySerializer serializer;
        private EventingBasicConsumer consumer;
        private readonly Random random;
        private string consumerTag;

        public PizzaOven(IModel model, JsonByteArraySerializer serializer, Random random)
        {
            this.model = model;
            this.serializer = serializer;
            this.random = random;
        }

        public void Register()
        {
            model.QueueDeclare(QueueName, true, false, false, null);
            model.QueueBind(QueueName, "pizza_ordered", "");
            model.ExchangeDeclare(PizzaBakedExchange, ExchangeType.Fanout, true);

            consumer = new EventingBasicConsumer(model);
            consumer.Received += PizzaOrderRecieved;
            consumerTag = model.BasicConsume(QueueName, false, consumer);
        }

        private void PizzaOrderRecieved(object sender, BasicDeliverEventArgs e)
        {
            var pizzaOrder = serializer.Deserialize<PizzaOrder>(e.Body);
            model.BasicAck(e.DeliveryTag, false);
            BakePizza(pizzaOrder);
        }

        private async void BakePizza(PizzaOrder pizzaOrder)
        {
            var bakeTime = pizzaOrder.Toppings.Length*random.Next(500, 1000);
            await Task.Delay(bakeTime);
            var finishedPizza = new PizzaBaked
            {
                OrderId = pizzaOrder.OrderId,
                PizzaId = pizzaOrder.PizzaId,
                Quality = random.NextDouble(),
                Size = pizzaOrder.Size,
                Toppings = pizzaOrder.Toppings,
            };
            var props = model.CreateBasicProperties();
            props.Persistent = false;
            props.ContentEncoding = "UTF8";
            model.BasicPublish(PizzaBakedExchange, "", props, serializer.Serialize(finishedPizza));
        }

        public void Deregister()
        {
            model.BasicCancel(consumerTag);
            consumer.Received -= PizzaOrderRecieved;
        }
    }
}
