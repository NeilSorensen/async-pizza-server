using System;
using System.Collections.Generic;
using System.Linq;
using PizzaHouse.Shared;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeliveryBoy
{
    public class OrderAssembler
    {
        private readonly IModel model;
        private readonly JsonByteArraySerializer serializer;
        private readonly CouponVerifier couponGenerator;
        private const string OrderAcceptedQueue = CashRegister.OrderAcceptedExchange + "=>order_assembler";
        private const string PizzaBakedQueue = PizzaOven.PizzaBakedExchange + "=>order_assembler";
        public const string OrderFilledExchange = "order_filled";

        private IDictionary<Guid, OrderAccepted> unfilledOrders = new Dictionary<Guid, OrderAccepted>();
        private IDictionary<Guid, List<PizzaBaked>> undeliveredPizzas = new Dictionary<Guid, List<PizzaBaked>>();

        private object orderCheckLock = new Object();

        private EventingBasicConsumer pizzaBakedConsumer;
        private EventingBasicConsumer orderConsumer;
        private string pizzaConsumerTag;
        private string orderConsumerTag;
        private const double RequiredQuality = 0.01;

        public OrderAssembler(IModel model, JsonByteArraySerializer serializer, CouponVerifier couponGenerator)
        {
            this.model = model;
            this.serializer = serializer;
            this.couponGenerator = couponGenerator;
        }

        public void Register()
        {
            model.ExchangeDeclare(OrderFilledExchange, ExchangeType.Fanout, true);
            model.QueueDeclare(OrderAcceptedQueue, true, false, false, null);
            model.QueueBind(OrderAcceptedQueue, CashRegister.OrderAcceptedExchange, "");

            model.QueueDeclare(PizzaBakedQueue, true, false, false, null);
            model.QueueBind(PizzaBakedQueue, PizzaOven.PizzaBakedExchange, "");

            pizzaBakedConsumer = new EventingBasicConsumer(model);
            pizzaBakedConsumer.Received += PizzaFinished;
            pizzaConsumerTag = model.BasicConsume(PizzaBakedQueue, false, pizzaBakedConsumer);


            orderConsumer = new EventingBasicConsumer(model);
            orderConsumer.Received += OrderStarted;
            orderConsumerTag = model.BasicConsume(OrderAcceptedQueue, false, orderConsumer);
        }

        private void OrderStarted(object sender, BasicDeliverEventArgs e)
        {
            var order = serializer.Deserialize<OrderAccepted>(e.Body);
            model.BasicAck(e.DeliveryTag, false);
            unfilledOrders.Add(order.OrderId, order);

            CheckOrderCompletion(order.OrderId);
        }

        private void PizzaFinished(object sender, BasicDeliverEventArgs e)
        {
            var pizza = serializer.Deserialize<PizzaBaked>(e.Body);
            model.BasicAck(e.DeliveryTag, false);
            if (pizza.Quality < RequiredQuality)
            {
                RequestPizzaAgain(pizza);
            }
            else
            {
                AddToHotbox(pizza);
                CheckOrderCompletion(pizza.OrderId);
            }

        }

        private void AddToHotbox(PizzaBaked pizza)
        {
            lock (undeliveredPizzas)
            {
                if (!undeliveredPizzas.ContainsKey(pizza.OrderId))
                {
                    undeliveredPizzas.Add(pizza.OrderId, new List<PizzaBaked>());
                }
                undeliveredPizzas[pizza.OrderId].Add(pizza);
            }
        }

        private void RequestPizzaAgain(PizzaBaked pizza)
        {
            var reorder = new PizzaOrder
            {
                OrderId = pizza.OrderId,
                PizzaId = pizza.PizzaId,
                Size = pizza.Size,
                Toppings = pizza.Toppings,
            };

            var basicProps = model.CreateBasicProperties();
            basicProps.Persistent = false;
            basicProps.ContentEncoding = "UTF8";
            model.BasicPublish(CashRegister.PizzaOrderedExchange, "", basicProps, serializer.Serialize(reorder));
        }

        private void CheckOrderCompletion(Guid orderId)
        {
            if (IsOrderComplete(orderId))
            {
                DeliverOrder(orderId);
            }
        }

        private void DeliverOrder(Guid orderId)
        {
            var order = unfilledOrders[orderId];
            var pizzas = undeliveredPizzas[orderId];
            var filledOrder = new OrderFilled
            {
                Address = order.Address,
                Customer = order.Customer,
                OrderPrice = order.OrderPrice,
                CouponCode = couponGenerator.GenerateCoupon(order.Customer),
                Pizzas = pizzas.Select(x => new Pizza {Size = x.Size, Toppings = x.Toppings}).ToList()
            };

            var basicProps = model.CreateBasicProperties();
            basicProps.Persistent = false;
            basicProps.ContentEncoding = "UTF8";
            model.BasicPublish(OrderFilledExchange, "", basicProps, serializer.Serialize(filledOrder));
        }

        private bool IsOrderComplete(Guid orderId)
        {
            lock (orderCheckLock)
            {
                if (!unfilledOrders.ContainsKey(orderId)) return false;
                if (!undeliveredPizzas.ContainsKey(orderId)) return false;

                var requiredPizzas = unfilledOrders[orderId].Pizzas;
                var undeliveredIds = new HashSet<Guid>(undeliveredPizzas[orderId].Select(p => p.PizzaId));
                if (requiredPizzas.All(p => undeliveredIds.Contains(p)))
                {
                    return true;
                }
            }
            return false;
        }

        public void Deregister()
        {
            model.BasicCancel(pizzaConsumerTag);
            model.BasicCancel(orderConsumerTag);

            pizzaBakedConsumer.Received -= PizzaFinished;
            orderConsumer.Received -= OrderStarted;
        }
    }
}
