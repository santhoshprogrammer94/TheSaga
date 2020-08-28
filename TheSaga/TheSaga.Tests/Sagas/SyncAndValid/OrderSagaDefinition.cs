﻿using System;
using System.Threading.Tasks;
using TheSaga.Builders;
using TheSaga.Models;
using TheSaga.Tests.Sagas.SyncAndValid.Activities;
using TheSaga.Tests.Sagas.SyncAndValid.EventHandlers;
using TheSaga.Tests.Sagas.SyncAndValid.Events;
using TheSaga.Tests.Sagas.SyncAndValid.States;

namespace TheSaga.Tests.Sagas.SyncAndValid
{
    public class OrderSagaDefinition : ISagaModelDefintion<OrderData>
    {
        public ISagaModel<OrderData> GetModel(IServiceProvider serviceProvider)
        {
            ISagaBuilder<OrderData> builder = new SagaBuilder<OrderData>(serviceProvider);

            builder.
                Start<OrderCreatedEvent, OrderCreatedEventHandler>("OrderCreatedEventStep0").
                    Then("OrderCreatedEventStep1", ctx => Task.CompletedTask).
                    TransitionTo<StateCreated>();

            builder.
                During<StateCreated>().
                When<OrderCompletedEvent>().
                    Then("OrderCompletedEventStep1", ctx => Task.CompletedTask).
                    Then<SendEmailToClientEvent>("email").
                    Then<SendMessageToTheManagerEvent>("SendMessageToTheManagerEventStep").
                    Then<OrderCourierEvent>("OrderCourierEventStep").
                    TransitionTo<StateCompleted>();

            builder.
                During<StateCompleted>().
                When<OrderSendEvent>().
                    Then("OrderSendEventStep1", ctx => Task.CompletedTask).
                    TransitionTo<StateOrderSend>();

            builder.
                During<StateOrderSend>().
                When<DeliveredEvent>().
                    Then("DeliveredEventStep1", ctx => Task.CompletedTask).
                    Finish();

            builder.
                During<StateCreated>().
                When<ToAlternative1Event>().                
                    TransitionTo<StateAlternative1>().
                When<ToAlternative2Event>().
                    TransitionTo<StateAlternative2>();

            return builder.
                Build();
        }
    }
}