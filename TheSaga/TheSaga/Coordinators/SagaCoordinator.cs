﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TheSaga.Events;
using TheSaga.Exceptions;
using TheSaga.Execution;
using TheSaga.Options;
using TheSaga.Persistance;
using TheSaga.Providers;
using TheSaga.Registrator;
using TheSaga.States;
using TheSaga.Utils;
using Microsoft.Extensions.DependencyInjection;
using TheSaga.Coordinators.Observables;
using TheSaga.Execution.Commands;
using TheSaga.Execution.Commands.Handlers;
using TheSaga.Locking;
using TheSaga.Messages;
using TheSaga.Messages.MessageBus;
using TheSaga.Models;
using TheSaga.SagaModels;
using TheSaga.ValueObjects;

namespace TheSaga.Coordinators
{
    public class SagaCoordinator : ISagaCoordinator
    {
        private IInternalMessageBus internalMessageBus;
        private ISagaPersistance sagaPersistance;
        private ISagaRegistrator sagaRegistrator;
        private IDateTimeProvider dateTimeProvider;
        private ISagaLocking sagaLocking;
        private IServiceProvider serviceProvider;
        public SagaCoordinator(ISagaRegistrator sagaRegistrator, ISagaPersistance sagaPersistance, IInternalMessageBus internalMessageBus, IDateTimeProvider dateTimeProvider, ISagaLocking sagaLocking, IServiceProvider serviceProvider)
        {
            this.sagaRegistrator = sagaRegistrator;
            this.sagaPersistance = sagaPersistance;
            this.internalMessageBus = internalMessageBus;
            this.dateTimeProvider = dateTimeProvider;
            this.sagaLocking = sagaLocking;
            this.serviceProvider = serviceProvider;

            new LockingObservable(serviceProvider).
                Subscribe();

            new ExecutionStartObservable(serviceProvider).
                Subscribe();

            new ExecutionEndObservable(serviceProvider).
                Subscribe();

            new AsyncStepCompletedObservable(serviceProvider).
                Subscribe();
        }

        public async Task<ISaga> Send(IEvent @event)
        {
            Type eventType = @event.GetType();
            SagaID sagaId = SagaID.From(@event.ID);

            ISagaModel model = sagaRegistrator.FindModelForEventType(eventType);
            if (model == null)
                throw new SagaEventNotRegisteredException(eventType);

            ISaga newSaga = await CreateNewSagaIfRequired(model, sagaId, eventType);

            try
            {
                ISaga saga =
                    newSaga ??
                    await sagaPersistance.Get(sagaId);

                await internalMessageBus.Publish(
                    new ExecutionStartMessage(saga));

                await sagaPersistance.
                    Set(saga);

                ExecuteSagaCommandHandler handler = serviceProvider.
                    GetRequiredService<ExecuteSagaCommandHandler>();

                //ISagaExecutor sagaExecutor = sagaRegistrator.
                //FindExecutorForStateType(model.GetType());

                return await handler.
                    Handle(new ExecuteSagaCommand()
                    {
                        Async = AsyncExecution.False(),
                        Event = @event,
                        ID = SagaID.From(saga.Data.ID),
                        Model = model
                    });
            }
            catch
            {
                if (newSaga != null)
                {
                    await sagaPersistance.
                        Remove(newSaga.Data.ID);
                }

                throw;
            }
        }

        public async Task WaitForState<TState>(Guid id, SagaWaitOptions waitOptions = null)
            where TState : IState, new()
        {
            if (waitOptions == null)
                waitOptions = new SagaWaitOptions();

            try
            {
                bool stateChanged = false;

                internalMessageBus.Subscribe<StateChangedMessage>(this, (mesage) =>
                {
                    if (mesage.SagaID == id &&
                        mesage.CurrentState == new TState().GetStateName())
                    {
                        stateChanged = true;
                    }
                    return Task.CompletedTask;
                });

                ISaga saga = await sagaPersistance.
                    Get(id);

                if (saga == null)
                    throw new SagaInstanceNotFoundException(id);

                if (saga.State.CurrentState == new TState().GetStateName())
                    return;

                Stopwatch stopwatch = Stopwatch.StartNew();
                while (!stateChanged)
                {
                    await Task.Delay(250);
                    if (stopwatch.Elapsed >= waitOptions.Timeout)
                        throw new TimeoutException();
                }
            }
            finally
            {
                internalMessageBus.Unsubscribe<StateChangedMessage>(this);
            }
        }

        private async Task<ISaga> CreateNewSagaIfRequired(ISagaModel model, SagaID id, Type eventType)
        {
            ISaga saga = null;

            if (eventType != null)
            {
                bool isStartEvent = model.
                    IsStartEvent(eventType);

                if (isStartEvent)
                    saga = await CreateNewSaga(model, id);
            }

            return saga;
        }

        private async Task<ISaga> CreateNewSaga(ISagaModel model, SagaID id)
        {
            if (id == SagaID.Empty())
                id = SagaID.New();

            ISagaData data = (ISagaData)Activator.CreateInstance(model.SagaStateType);
            data.ID = id;

            ISaga saga = new Saga()
            {
                Data = data,
                Info = new SagaInfo()
                {
                    Created = dateTimeProvider.Now,
                    Modified = dateTimeProvider.Now
                },
                State = new SagaState()
                {
                    CurrentState = new SagaStartState().GetStateName(),
                    CurrentStep = null
                }
            };

            return saga;
        }

    }
}