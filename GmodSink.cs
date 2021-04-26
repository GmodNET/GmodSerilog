using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;

namespace GmodNET.Serilog.Sink
{
    public class GmodSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            throw new NotImplementedException();
        }
    }
}
