﻿using System.Threading.Tasks;
using TheSaga.Events;
using TheSaga.Models.Context;
using TheSaga.Tests.Sagas.SyncAndValid.Events;

namespace TheSaga.Tests.Sagas.SyncAndValid.EventHandlers
{
    public class OrderCreatedEventHandler : IEventHandler<OrderData, OrderCreatedEvent>
    {
        public OrderCreatedEventHandler()
        {
        }

        public Task Compensate(IExecutionContext<OrderData> context, OrderCreatedEvent @event)
        {
            return Task.CompletedTask;
        }

        public Task Execute(IExecutionContext<OrderData> context, OrderCreatedEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}