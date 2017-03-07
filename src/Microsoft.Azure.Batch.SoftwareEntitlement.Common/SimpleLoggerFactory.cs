using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Events;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Static factory for creating a logger
    /// </summary>
    public static class SimpleLoggerFactory
    {
        // Reference to our shared logger for global use
        private static ISimpleLogger _logger;

        public static ISimpleLogger Logger 
            => _logger ?? throw new InvalidOperationException("Logging has not been initialized.");

        /// <summary>
        /// Creates a configured logger to use within SesTest
        /// </summary>
        /// <remarks>Also caches a copy of the logger for later reference from elsewhere.</remarks>
        /// <param name="level">Desired logging level</param>
        /// <returns>Instance of simple logger.</returns>
        public static ISimpleLogger CreateLogger(LogEventLevel level)
        {
            var actualLogger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Is(level)
                .CreateLogger();

            _logger = new SerilogLogger(actualLogger);

            return _logger;
        }
    }
}
