﻿using System;
using TheSaga.ModelsSaga.Actions;
using TheSaga.ModelsSaga.Actions.Interfaces;
using TheSaga.ModelsSaga.Interfaces;

namespace TheSaga.ModelsSaga
{
    internal class SagaModel : ISagaModel
    {
        public SagaModel(Type SagaStateType)
        {
            this.Name = $"{SagaStateType.Name}Model";
            this.SagaStateType = SagaStateType;
            this.HistoryPolicy = ESagaHistoryPolicy.StoreOnlyCurrentStep;
            this.ResumePolicy = ESagaResumePolicy.DoCurrentStepCompensation;
            this.Actions = new SagaActions();
        }

        public ISagaActions Actions { get; }
        public ESagaHistoryPolicy HistoryPolicy { get; set; }
        public string Name { get; set; }
        public ESagaResumePolicy ResumePolicy { get; set; }
        public Type SagaStateType { get; }
    }
}
