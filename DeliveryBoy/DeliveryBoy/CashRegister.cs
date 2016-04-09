using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PizzaHouse.Shared;
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
        private readonly CouponVerifier couponVerifier;
        private decimal BasePrice = 5.50M;

        public const string OrderExchangeName = "order_placed";
        private const string QueueName = OrderExchangeName + "=>cash_register";
        public const string PizzaOrderedExchange = "pizza_ordered";
        public const string OrderAcceptedExchange = "order_accepted";


        public CashRegister(IModel model, JsonByteArraySerializer serializer, CouponVerifier couponVerifier)
        {
            this.model = model;
            this.serializer = serializer;
            this.couponVerifier = couponVerifier;
        }

        public void Register()
        {
            model.ExchangeDeclare(OrderExchangeName, ExchangeType.Fanout, true);
            model.QueueDeclare(QueueName, true, false, false, null);
            model.QueueBind(QueueName, OrderExchangeName, "");
            model.ExchangeDeclare(PizzaOrderedExchange, ExchangeType.Fanout, true);
            model.ExchangeDeclare(OrderAcceptedExchange, ExchangeType.Fanout, true);

            consumer = new EventingBasicConsumer(model);
            consumer.Received += OrderRecieved;
            consumerTag = model.BasicConsume(QueueName, false, consumer);
        }

        private void OrderRecieved(object sender, BasicDeliverEventArgs e)
        {
            var orderId = Guid.NewGuid();
            OrderPlaced order = null;
            try
            {
                order = serializer.Deserialize<OrderPlaced>(e.Body);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unprocessable Order! " + Encoding.UTF8.GetString(e.Body).Replace("\n", " "));
                return;
            }
            finally
            {
                model.BasicAck(e.DeliveryTag, false);
            }
            var orderPrice = CalculateOrderCost(order);
            var preparedPizzas = order.Pizzas.ToDictionary(x => Guid.NewGuid(), x => x);
            AcceptOrder(orderId, orderPrice, preparedPizzas.Keys.ToArray(), order);
            OrderPizzas(preparedPizzas, orderId);
        }

        private void AcceptOrder(Guid orderId, decimal orderPrice, Guid[] pizzaIds, OrderPlaced order)
        {
            var accepted = new OrderAccepted
            {
                OrderId = orderId,
                Address = order.Address,
                Customer = order.Customer,
                OrderPrice = orderPrice,
                Pizzas = pizzaIds
            };

            var props = model.CreateBasicProperties();
            props.Persistent = false;
            props.ContentEncoding = "UTF8";
            model.BasicPublish(OrderAcceptedExchange, "", props, serializer.Serialize(accepted));
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
            props.Persistent = false;
            props.ContentEncoding = "UTF8";
            model.BasicPublish(PizzaOrderedExchange, "", props, serializer.Serialize(order));
        }

        private decimal CalculateOrderCost(OrderPlaced order)
        {
            var couponMultiplier = couponVerifier.CouponIsValid(order.Coupon, order.Customer) ? .5M : 1M;
            return (order.Pizzas.Select(CalculatePizzaCost).Sum() + 2.8M) * couponMultiplier;
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

        private decimal GetToppingPrice(string topping)
        {
            return 1.50M;
        }

        public void Deregister()
        {
            model.BasicCancel(consumerTag);
            consumer.Received -= OrderRecieved;
        }
    }
}
