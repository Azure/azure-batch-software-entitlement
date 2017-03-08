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

        // Reference to the actual serilog logger
        private static ILogger _serilogger;

        public static ISimpleLogger SimpleLogger 
            => _logger ?? throw new InvalidOperationException("Logging has not been initialized.");

        public static ILogger Serilogger
            => _serilogger ?? throw new InvalidOperationException("Logging has not been initialized.");

        /// <summary>
        /// Creates a configured logger to use within SesTest
        /// </summary>
        /// <remarks>Also caches a copy of the logger for later reference from elsewhere.</remarks>
        /// <param name="level">Desired logging level</param>
        /// <returns>Instance of simple logger.</returns>
        public static ISimpleLogger CreateLogger(LogEventLevel level)
        {
            _serilogger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Is(level)
                .CreateLogger();

            _logger = new SerilogLogger(_serilogger);

            return _logger;
        }
    }
}
