using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;
using TheSaga.Coordinators;
using TheSaga.Events;
using TheSaga.Persistance;
using TheSaga.Registrator;
using TheSaga.SagaStates;
using TheSaga.Tests.Sagas.SyncAndInvalidSaga;
using TheSaga.Tests.Sagas.SyncAndInvalidSaga.Events;
using TheSaga.Tests.Sagas.SyncAndInvalidSaga.Exceptions;
using TheSaga.Tests.Sagas.SyncAndInvalidSaga.States;
using Xunit;

namespace TheSaga.Tests
{
    public class SyncAndInvalidSagaTests
    {
        [Fact]
        public async Task WHEN_compensationThrowsErrorOnUpdate_THEN_sagaShouldBeInValidState()
        {
            // given
            ISagaState sagaState = await sagaCoordinator.
                Send(new ValidCreatedEvent());

            // when
            await Assert.ThrowsAsync<TestCompensationException>(async () =>
            {
                await sagaCoordinator.Send(new InvalidCompensationEvent()
                {
                    CorrelationID = sagaState.CorrelationID
                });
            });

            // then
            InvalidSagaState persistedState = (InvalidSagaState)await sagaPersistance.
                Get(sagaState.CorrelationID);

            persistedState.ShouldNotBeNull();
            persistedState.CurrentStep.ShouldBe(null);
            persistedState.CurrentState.ShouldBe(nameof(StateCreated));
            persistedState.CurrentError.ShouldNotBeNull();
            persistedState.CorrelationID.ShouldBe(sagaState.CorrelationID);

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidCompensationEventStep1");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidCompensationEventStep1");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidCompensationEventStep2");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidCompensationEventStep2");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidCompensationEventStep3");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidCompensationEventStep3");
        }

        [Fact]
        public async Task WHEN_sagaThrowsErrorOnStart_THEN_sagaShouldNotExists()
        {
            // given
            Guid correlationID = Guid.NewGuid();
            IEvent startEvent = new InvalidCreatedEvent()
            {
                CorrelationID = correlationID
            };

            // when
            await Assert.ThrowsAsync<TestSagaException>(async () =>
            {
                ISagaState sagaState = await sagaCoordinator.
                    Send(startEvent);
            });

            // then
            ISagaState persistedState = await sagaPersistance.Get(correlationID);
            persistedState.ShouldBeNull();
        }

        [Fact]
        public async Task WHEN_sagaThrowsErrorOnStart_THEN_sagaStepExceptionIsThrown()
        {
            // given
            Guid correlationID = Guid.NewGuid();
            IEvent startEvent = new InvalidCreatedEvent()
            {
                CorrelationID = correlationID
            };

            // then
            await Assert.ThrowsAsync<TestSagaException>(async () =>
            {
                // when
                ISagaState sagaState = await sagaCoordinator.
                    Send(startEvent);
            });
        }

        [Fact]
        public async Task WHEN_sagaThrowsErrorOnUpdate_THEN_sagaShouldBeInValidState()
        {
            // given
            ISagaState sagaState = await sagaCoordinator.
                Send(new ValidCreatedEvent());

            // when
            await Assert.ThrowsAsync<TestSagaException>(async () =>
            {
                await sagaCoordinator.Send(new InvalidUpdateEvent()
                {
                    CorrelationID = sagaState.CorrelationID
                });
            });

            // then
            InvalidSagaState persistedState = (InvalidSagaState)await sagaPersistance.
                Get(sagaState.CorrelationID);

            persistedState.ShouldNotBeNull();
            persistedState.CurrentStep.ShouldBe(null);
            persistedState.CurrentState.ShouldBe(nameof(StateCreated));
            persistedState.CurrentError.ShouldNotBeNull();
            persistedState.CorrelationID.ShouldBe(sagaState.CorrelationID);

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidUpdateEvent1");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidUpdateEvent1");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidUpdateEvent2");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidUpdateEvent2");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == true && item.StepName == "InvalidUpdateEvent3");

            persistedState.History.ShouldContain(item =>
                item.IsCompensating == false && item.StepName == "InvalidUpdateEvent3");
        }

        [Fact]
        public async Task WHEN_sendValidStateToSagaWithError_THEN_errorShouldBeNull()
        {
            // given
            ISagaState sagaState = await sagaCoordinator.
                Send(new ValidCreatedEvent());

            await Assert.ThrowsAsync<TestCompensationException>(async () =>
            {
                await sagaCoordinator.Send(new InvalidCompensationEvent()
                {
                    CorrelationID = sagaState.CorrelationID
                });
            });

            // then
            InvalidSagaState persistedState2 = (InvalidSagaState)await sagaPersistance.
                Get(sagaState.CorrelationID);

            // when
            await sagaCoordinator.Send(new ValidUpdateEvent()
            {
                CorrelationID = sagaState.CorrelationID
            });

            // then
            InvalidSagaState persistedState = (InvalidSagaState)await sagaPersistance.
                Get(sagaState.CorrelationID);

            persistedState.ShouldNotBeNull();
            persistedState.CurrentStep.ShouldBe(null);
            persistedState.CurrentState.ShouldBe(nameof(StateUpdated));
            persistedState.CurrentError.ShouldBeNull();
            persistedState.CorrelationID.ShouldBe(sagaState.CorrelationID);
        }

        #region Arrange

        private ISagaCoordinator sagaCoordinator;
        private ISagaPersistance sagaPersistance;
        private ISagaRegistrator sagaRegistrator;
        private IServiceProvider serviceProvider;

        public SyncAndInvalidSagaTests()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddTheSaga();

            serviceProvider = services.BuildServiceProvider();

            sagaRegistrator = serviceProvider.
                GetRequiredService<ISagaRegistrator>();

            sagaPersistance = serviceProvider.
                GetRequiredService<ISagaPersistance>();

            sagaCoordinator = serviceProvider.
                GetRequiredService<ISagaCoordinator>();

            sagaRegistrator.Register(
                new InvalidSagaDefinition().GetModel(serviceProvider));
        }

        #endregion Arrange
    }
}