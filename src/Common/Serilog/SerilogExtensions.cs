using Serilog;
using Serilog.Configuration;

namespace Common.Serilog
{
    public static class SerilogExtensions
    {
        /// <summary>
        /// Add support for logging <see cref="System.Diagnostics.Activity"/> trace
        /// ids to Serilog.
        /// </summary>
        public static LoggerConfiguration WithSerilogActivity(this LoggerEnrichmentConfiguration enrich)
        {
            return enrich is null ? throw new ArgumentNullException(nameof(enrich)) : enrich.With<SerilogActivityEnricher>();
        }
    }
}
