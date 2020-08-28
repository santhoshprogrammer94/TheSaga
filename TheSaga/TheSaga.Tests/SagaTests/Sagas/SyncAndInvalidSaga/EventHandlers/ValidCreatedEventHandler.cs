﻿using System.Threading.Tasks;
using TheSaga.Coordinators;
using TheSaga.Events;
using TheSaga.ExecutionContext;
using TheSaga.Tests.SagaTests.Sagas.SyncAndInvalidSaga.Events;

namespace TheSaga.Tests.SagaTests.Sagas.SyncAndInvalidSaga.EventHandlers
{
    public class ValidCreatedEventHandler : IEventHandler<InvalidSagaData, ValidCreatedEvent>
    {
        private readonly ISagaCoordinator sagaCoordinator;

        public ValidCreatedEventHandler(ISagaCoordinator sagaCoordinator)
        {
            this.sagaCoordinator = sagaCoordinator;
        }

        public Task Compensate(IExecutionContext<InvalidSagaData> context, ValidCreatedEvent @event)
        {
            return Task.CompletedTask;
        }

        public Task Execute(IExecutionContext<InvalidSagaData> context, ValidCreatedEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}