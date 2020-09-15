using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheSaga.Coordinators;
using TheSaga.Locking;
using TheSaga.Models;
using TheSaga.Models.Interfaces;
using TheSaga.Persistance;
using TheSaga.Tests.SagaTests.ResumeSaga.Events;
using Xunit;
using TheSaga.Persistance.SqlServer;
using TheSaga.Persistance.SqlServer.Options;
using TheSaga.Locking.DistributedLock;
using TheSaga.Tests.SagaTests.ResumeSaga.States;
using TheSaga.Locking.DistributedLock.Options;

namespace TheSaga.Tests.SagaTests.ResumeSaga
{
    public class ResumeTests
    {
        [Fact]
        public async Task WHEN_sagaIsStopped_THEN_sagaShouldNotBeInIdleState()
        {
            // given
            ResumeSagaSettings.StopSagaExecution = true;
            ISaga saga = await sagaCoordinator.Publish(new CreateEvent());

            // when
            await sagaCoordinator.Publish(new ResumeSagaUpdateEvent() { ID = saga.Data.ID });

            // then
            ISaga persistedSaga = await sagaPersistance.Get(saga.Data.ID);
            persistedSaga.IsIdle().ShouldBeFalse();
        }

        [Fact]
        public async Task WHEN_sagaIsStoppedOnCreation_THEN_resumingSagaToInitState()
        {
            // given
            Guid id = Guid.NewGuid();
            ResumeSagaSettings.StopSagaExecution = true;
            await sagaCoordinator.Publish(new CreateWithBreakEvent() { ID = id });
            await sagaLocking.Banish(id);

            // when
            ResumeSagaSettings.StopSagaExecution = false;
            await sagaCoordinator.
                Resume(id);

            // then
            ISaga persistedSaga = await sagaPersistance.Get(id);
            persistedSaga.IsIdle().ShouldBeTrue();
            persistedSaga.ExecutionState.CurrentState.ShouldBe(nameof(Init));
            persistedSaga.ExecutionState.History.Count.ShouldBe(3);
        }

        [Fact]
        public async Task WHEN_sagaIsStoppedOnInvalidCreation_THEN_resumingShouldRemoveSaga()
        {
            // given
            Guid id = Guid.NewGuid();
            ResumeSagaSettings.StopSagaExecution = true;
            await sagaCoordinator.Publish(new CreateWithErrorEvent() { ID = id });
            await sagaLocking.Banish(id);

            // when
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                ResumeSagaSettings.StopSagaExecution = false;
                await sagaCoordinator.
                    Resume(id);
            });

            // then
            ISaga persistedSaga = await sagaPersistance.Get(id);
            persistedSaga.IsIdle().ShouldBeTrue();
            persistedSaga.ExecutionState.IsDeleted.ShouldBeTrue();
            persistedSaga.ExecutionState.CurrentState.ShouldBe("");
            persistedSaga.ExecutionState.History.Count.ShouldBe(3);
        }

        [Fact]
        public async Task WHEN_sagaIsStopped_THEN_sagaLockShouldBeAcquired()
        {
            // given
            ResumeSagaSettings.StopSagaExecution = true;
            ISaga saga = await sagaCoordinator.Publish(new CreateEvent());

            // when
            await sagaCoordinator.Publish(new ResumeSagaUpdateEvent() { ID = saga.Data.ID });

            // then
            (await sagaLocking.IsAcquired(saga.Data.ID)).
                ShouldBeTrue();
        }

        [Fact]
        public async Task WHEN_stoppedSagaIsResumed_THEN_sagaShouldMoveToValidState()
        {
            // given
            ResumeSagaSettings.StopSagaExecution = true;
            ISaga saga = await sagaCoordinator.Publish(new CreateEvent());
            await sagaCoordinator.Publish(new ResumeSagaUpdateEvent() { ID = saga.Data.ID });
            ResumeSagaSettings.StopSagaExecution = false;
            await sagaLocking.Banish(saga.Data.ID);

            // when
            await sagaCoordinator.
                Resume(saga.Data.ID);

            // then
            ISaga persistedSaga = await sagaPersistance.Get(saga.Data.ID);
            persistedSaga.IsIdle().ShouldBeTrue();
        }

        [Fact]
        public async Task WHEN_stoppedSagaIsResumed_THEN_sagaShouldCompensateAndExecuteLastStep()
        {
            // given
            ResumeSagaSettings.StopSagaExecution = true;
            ISaga saga = await sagaCoordinator.Publish(new CreateEvent());
            await sagaCoordinator.Publish(new ResumeSagaUpdateEvent() { ID = saga.Data.ID });
            ResumeSagaSettings.StopSagaExecution = false;
            await sagaLocking.Banish(saga.Data.ID);

            // when
            await sagaCoordinator.
                Resume(saga.Data.ID);

            // then
            ISaga persistedSaga = await sagaPersistance.Get(saga.Data.ID);
            persistedSaga.ExecutionState.History.Where(s => s.ResumeData != null).Count().ShouldBe(1);
            persistedSaga.ExecutionState.History.Where(s => s.CompensationData != null).Count().ShouldBe(0);
            persistedSaga.ExecutionState.History.Where(s => s.ExecutionData != null).Count().ShouldBe(3);
            persistedSaga.IsIdle().ShouldBeTrue();
        }

        #region Arrange

        private ISagaCoordinator sagaCoordinator;
        private ISagaPersistance sagaPersistance;
        private ISagaLocking sagaLocking;

        public ResumeTests()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSaga(cfg =>
            {
#if SQL_SERVER
                cfg.UseSqlServer(new SqlServerOptions()
                {
                    ConnectionString = "data source=lab16;initial catalog=ziarno;uid=dba;pwd=sql;"
                });
                cfg.UseDistributedLock(new SqlServerLockingOptions()
                {
                    ConnectionString = "data source=lab16;initial catalog=ziarno;uid=dba;pwd=sql;"
                });
#endif
            });

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            sagaPersistance = serviceProvider.
                GetRequiredService<ISagaPersistance>();

            sagaCoordinator = serviceProvider.
                GetRequiredService<ISagaCoordinator>();

            sagaLocking = serviceProvider.
                GetRequiredService<ISagaLocking>();
        }

        #endregion Arrange
    }
}
