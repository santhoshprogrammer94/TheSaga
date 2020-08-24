﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using TheSaga.Activities;
using TheSaga.Interfaces;
using TheSaga.Models;
using TheSaga.States;
using TheSaga.States.Actions;

namespace TheSaga.Builders
{
    public class SagaBuilder<TSagaState> : ISagaBuilder<TSagaState> where TSagaState : ISagaState
    {
        SagaModel<TSagaState> model;

        Type currentEvent;

        String currentState;

        public SagaBuilder()
        {
            model = new SagaModel<TSagaState>();
        }

        public SagaBuilder<TSagaState> After(TimeSpan time)
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    $"{currentState}_{nameof(After)}",
                    ctx => Task.Delay(time)));

            return this;
        }

        public SagaModel<TSagaState> Build()
        {
            return model;
        }

        public SagaBuilder<TSagaState> During<TState>()
            where TState : IState
        {
            currentState = typeof(TState).Name;
            currentEvent = null;
            return this;
        }

        public SagaBuilder<TSagaState> Start<TEvent>()
            where TEvent : IEvent
        {
            currentState = null;
            currentEvent = typeof(TEvent);
            model.Actions.Add(new SagaAction<TSagaState>()
            {
                State = currentState,
                Event = typeof(TEvent)
            });
            return this;
        }

        public SagaBuilder<TSagaState> Then<TSagaActivity>() where TSagaActivity : ISagaActivity<TSagaState>
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    $"{currentState}_{nameof(Then)}",
                    typeof(TSagaActivity)));

            return this;
        }

        public SagaBuilder<TSagaState> Then<TSagaActivity>(String stepName) where TSagaActivity : ISagaActivity<TSagaState>
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    stepName,
                    typeof(TSagaActivity)));

            return this;
        }

        public SagaBuilder<TSagaState> Then(ThenActionDelegate<TSagaState> action)
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    $"{currentState}_{nameof(Then)}",
                    (ctx) => action((IInstanceContext<TSagaState>)ctx)));

            return this;
        }

        public SagaBuilder<TSagaState> Then(String stepName, ThenActionDelegate<TSagaState> action)
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    stepName,
                    action));

            return this;
        }

        public SagaBuilder<TSagaState> TransitionTo<TState>() where TState : IState
        {
            model.FindAction(currentState, currentEvent).Steps.Add(
                new SagaStep<TSagaState>(
                    $"{currentState}_{nameof(TransitionTo)}_{typeof(TState).Name}",
                    ctx =>
                    {
                        ctx.State.CurrentState = typeof(TState).Name;
                        return Task.CompletedTask;
                    }));
            return this;
        }

        public SagaBuilder<TSagaState> When<TEvent>() where TEvent : IEvent
        {
            currentEvent = typeof(TEvent);
            model.Actions.Add(new SagaAction<TSagaState>()
            {
                State = currentState,
                Event = currentEvent
            });
            return this;
        }

        public SagaBuilder<TSagaState> When<TEvent, TEventHandler>() where TEvent : IEvent
            where TEventHandler : IEventHandler<TSagaState, TEvent>
        {
            currentEvent = typeof(TEvent);
            model.Actions.Add(new SagaAction<TSagaState>()
            {
                State = currentState,
                Event = currentEvent,
                Steps = new List<ISagaStep>
                {
                    new SagaStep<TSagaState>(
                        $"{currentState}_{nameof(When)}_{typeof(TEvent).Name}",
                        typeof(TEventHandler))
                }
            }); ;
            return this;
        }
    }
}