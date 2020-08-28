﻿using System;
using System.Threading.Tasks;
using TheSaga.Builders;
using TheSaga.SagaModels;
using TheSaga.Tests.Sagas.SendTests.Events;
using TheSaga.Tests.Sagas.SendTests.States;

namespace TheSaga.Tests.Sagas.SendTests
{
    public class SendTestsBuilder : ISagaModelBuilder<SendTestsData>
    {

        ISagaBuilder<SendTestsData> builder;

        public SendTestsBuilder(ISagaBuilder<SendTestsData> builder)
        {
            this.builder = builder;
        }

        public ISagaModel<SendTestsData> Build()
        {
            builder.
                Name(nameof(SendTestsBuilder));

            builder.
                Start<SendCreateEvent>().
                TransitionTo<Init>();

            builder.
                Start<SendAlternativeCreateEvent>().
                TransitionTo<AkternativeInit>();

            builder.
                During<Init>().
                When<TestSendActionEvent>().
                Publish<SendAlternativeCreateEvent>(async (ctx, ev) =>
                    ctx.Data.CreatedSagaID = ev.ID = Guid.NewGuid()).
                TransitionTo<AfterInit>();

            return builder.
                Build();
        }
    }
}