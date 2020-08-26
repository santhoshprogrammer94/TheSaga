﻿using System;
using System.Collections.Generic;
using TheSaga.SagaStates;

namespace TheSaga.Tests.Sagas.SyncAndValid
{
    public class OrderState : ISagaState
    {
        public OrderState()
        {
            History = new List<SagaStepHistory>();
        }

        public Guid CorrelationID { get; set; }
        public Exception CurrentError { get; set; }
        public string CurrentState { get; set; }
        public string CurrentStep { get; set; }
        public IList<SagaStepHistory> History { get; set; }
        public bool IsCompensating { get; set; }
    }
}