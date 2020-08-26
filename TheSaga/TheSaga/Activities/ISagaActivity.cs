﻿using System.Threading.Tasks;
using TheSaga.Execution.Context;
using TheSaga.SagaStates;

namespace TheSaga.Activities
{
    public interface ISagaActivity<TSagaData>
        where TSagaData : ISagaData
    {
        Task Compensate(IExecutionContext<TSagaData> context);

        Task Execute(IExecutionContext<TSagaData> context);
    }
}