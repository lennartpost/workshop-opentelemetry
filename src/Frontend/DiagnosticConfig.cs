using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Frontend
{
    public static class DiagnosticsConfig
    {
        public const string ServiceName = "Frontend Service";
        public static ActivitySource ActivitySource = new(ServiceName);

        public static Meter Meter = new(ServiceName);
        public static Counter<long> RequestCounter = Meter.CreateCounter<long>("app.frontend.request_counter");
    }
}
