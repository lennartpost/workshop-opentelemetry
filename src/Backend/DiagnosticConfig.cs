using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Backend
{
    public static class DiagnosticsConfig
    {
        public const string ServiceName = "Backend Service";
        public static ActivitySource ActivitySource = new(ServiceName);

        public static Meter Meter = new(ServiceName);
        public static Counter<long> RequestCounter = Meter.CreateCounter<long>("app.backend.request_counter");
    }
}
