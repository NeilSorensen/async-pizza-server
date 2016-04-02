using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliveryBoy
{
    public class CashRegister
    {
        private IModel model;
        private EventingBasicConsumer consumer;
        private string consumerTag;
        private JsonByteArraySerializer serializer;
        private decimal BasePrice = 5.50M;

        public const string OrderExchangeName = "ordered_placed";
        private const string QueueName = OrderExchangeName + "=>cash_register";
        public const string PizzaOrderedExchange = "pizza_ordered";
        public const string OrderAcceptedExchange = "order_accepted";


        public CashRegister(IModel model, JsonByteArraySerializer serializer)
        {
            this.model = model;
            this.serializer = serializer;
        }

        public void Register()
        {
            model.QueueDeclare(QueueName, true, false, false, null);
            model.QueueBind(QueueName, OrderExchangeName, "");
            model.ExchangeDeclare(PizzaOrderedExchange, ExchangeType.Fanout, true);

            consumer = new EventingBasicConsumer(model);
            consumer.Received += OrderRecieved;
            consumerTag = model.BasicConsume(QueueName, false, consumer);
        }

        private void OrderRecieved(object sender, BasicDeliverEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OrderPizzas(IDictionary<Guid,Pizza> pizzas, Guid orderId)
        {
            pizzas.AsParallel().ForAll(x => OrderPizza(x.Key, x.Value, orderId));
        }

        private void OrderPizza(Guid pizzaId, Pizza toOrder, Guid orderId)
        {
            var order = new PizzaOrder()
            {
                OrderId = orderId,
                PizzaId = pizzaId,
                Size = toOrder.Size,
                Toppings = toOrder.Toppings
            };

            var props = model.CreateBasicProperties();
            props.Persistent = true;
            props.ContentEncoding = "UTF8";
            model.BasicPublish(PizzaOrderedExchange, "", props, serializer.Serialize(order));
        }

        private decimal CalculateOrderCost(IEnumerable<Pizza> pizzas)
        {
            return pizzas.Select(CalculatePizzaCost).Sum() + 2.8M;
        }

        private decimal CalculatePizzaCost(Pizza pizza)
        {
            decimal toppingValue = pizza.Toppings.Select(GetToppingPrice).Sum();
            return (toppingValue + BasePrice) * GetSizeMultiplier(pizza.Size);
        }

        private decimal GetSizeMultiplier(PizzaSize size)
        {
            switch (size)
            {
                case PizzaSize.Personal:
                    return 0.8M;
                case PizzaSize.Small:
                    return 0.9M;
                case PizzaSize.Medium:
                    return 1M;
                case PizzaSize.Large:
                    return 1.5M;
                case PizzaSize.XLarge:
                    return 2M;
            }
            return 10M;
        }

        public decimal GetToppingPrice(string topping)
        {
            return 1.50M;
        }
    }
}
