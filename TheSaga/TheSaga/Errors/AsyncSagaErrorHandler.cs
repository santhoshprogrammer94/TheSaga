﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TheSaga.Models;
using TheSaga.Models.Interfaces;

namespace TheSaga.Errors
{
    public class AsyncSagaErrorHandler : IAsyncSagaErrorHandler
    {
        public Task Handle(ISaga saga, Exception error)
        {
            return Task.CompletedTask;
        }
    }
}
