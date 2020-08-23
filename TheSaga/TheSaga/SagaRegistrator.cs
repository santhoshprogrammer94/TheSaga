﻿using System;
using System.Collections.Generic;
using TheSaga.Model;

namespace TheSaga
{
    public class SagaRegistrator : ISagaRegistrator
    {
        //public Dictionary<string, SagaModel> models;

        public SagaRegistrator()
        {
            //models = new Dictionary<string, SagaModel>();
        }

        /*public void Register<TSagaType, TState>(string sagaName) 
            where TSagaType : ISaga<TState>
            where TState : IData
        {
            ISagaBuilder<TSagaType> sagaBuilder = new SagaBuilder();
        }*/

        public void Register<TSagaType, TSataState>(string sagaName)
            where TSagaType : ISaga<TSataState>
            where TSataState : ISagaState
        {
            throw new NotImplementedException();
        }
    }
}