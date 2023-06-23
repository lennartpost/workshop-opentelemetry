using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Common.Serilog
{
    /// <summary>
    /// In net6.0 the traceparent (https://www.w3.org/TR/trace-context/#traceparent-header) is not picked up by Serilog. This
    /// enricher will solve that. In a future release of Serilog will this be solved by the library itself. Then is enricher 
    /// can be removed.
    /// </summary>
    public class SerilogActivityEnricher : ILogEventEnricher
    {
        ///<inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (Activity.Current is null) return;

            var activity = Activity.Current;

            log("SpanId", activity.SpanId);
            log("TraceId", activity.TraceId);
            if (activity.ParentId is not null) log("ParentId", activity.ParentId);

            void log(string name, object value) =>
                logEvent.AddPropertyIfAbsent(new LogEventProperty(name, new ScalarValue(value)));
        }
    }
}
