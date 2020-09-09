using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;
using TheSaga.Coordinators;
using TheSaga.Locking;
using TheSaga.Locking.DistributedLock;
using TheSaga.Locking.DistributedLock.Options;
using TheSaga.Models;
using TheSaga.Persistance;
using TheSaga.Persistance.SqlServer;
using TheSaga.Persistance.SqlServer.Options;
using TheSaga.Tests.SagaTests.Sagas.ChildStepsSaga;
using TheSaga.Tests.SagaTests.Sagas.ChildStepsSaga.Events;
using Xunit;

namespace TheSaga.Tests.SagaTests
{
    public class ChildStepsTests
    {
        [Fact]
        public async Task WHEN_sagaIsStopped_THEN_sagaShouldNotBeInIdleState()
        {
            // given
            var @event = new ChildStepsSagaCreateEvent();

            // when
            ISaga saga = await sagaCoordinator.Publish(@event);

            // then
            ISaga persistedSaga = await sagaPersistance.Get(saga.Data.ID);
            persistedSaga.IsIdle().ShouldBeTrue();
            persistedSaga.State.History.Count.ShouldBe(7);
        }


        #region Arrange

        private ISagaCoordinator sagaCoordinator;
        private ISagaPersistance sagaPersistance;
        private ISagaLocking sagaLocking;

        public ChildStepsTests()
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