﻿using System;
using System.Threading.Tasks;
using TheSaga.Events;
using TheSaga.Options;
using TheSaga.SagaStates;
using TheSaga.States;

namespace TheSaga.Coordinators
{
    public interface ISagaCoordinator
    {
        Task<ISaga> Send(IEvent @event);

        Task WaitForState<TState>(Guid id, SagaWaitOptions waitOptions = null)
            where TState : IState, new();
    }
}