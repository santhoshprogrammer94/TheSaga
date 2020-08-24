﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheSaga.Builders;
using TheSaga.Interfaces;
using TheSaga.Models;
using TheSaga.Persistance;
using TheSaga.States;
using TheSaga.States.Actions;

namespace TheSaga.Executors
{
    public class SagaExecutor<TSagaState> : ISagaExecutor
        where TSagaState : ISagaState
    {
        ISagaPersistance sagaPersistance;

        public SagaExecutor(ISagaPersistance sagaPersistance)
        {
            this.sagaPersistance = sagaPersistance;
        }

        public async Task<ISagaState> Start(ISagaModel model, IEvent @event)
        {
            Guid correlationID = @event.CorrelationID;
            if (correlationID == Guid.Empty)
                correlationID = Guid.NewGuid();

            ISagaState newSagaState = (ISagaState)Activator.CreateInstance(model.SagaStateType);
            newSagaState.CorrelationID = correlationID;
            await sagaPersistance.Set(newSagaState);

            return await Handle(model, @event);
        }

        public async Task<ISagaState> Handle(ISagaModel model, IEvent @event)
        {
            while (true)
            {
                ISagaState sagaState = await ExecuteStep(@event.CorrelationID, model, @event);
                if (sagaState.CurrentStep == null)
                    return sagaState;
            }
        }

        public async Task<ISagaState> ExecuteStep(Guid correlationID, ISagaModel model, IEvent @event)
        {
            ISagaState state = await sagaPersistance.Get(correlationID);

            IList<ISagaAction> actions = model.FindActions(state.CurrentState);
            ISagaAction action = actions.FirstOrDefault(a => a.Event == @event.GetType());
            ISagaStep step = action.FindStep(state.CurrentStep);

            try
            {
                IInstanceContext context = new InstanceContext<TSagaState>()
                {
                    State = (TSagaState)state
                };
                await step.Execute(context, @event);

                ISagaStep nextStep = action.
                    FindNextAfter(step);

                if (nextStep != null)
                {
                    state.CurrentStep = nextStep.StepName;
                }
                else
                {
                    state.CurrentStep = null;
                }

                await sagaPersistance.Set(state);

                return state;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}