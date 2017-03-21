using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;
using System.IO;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Static factory for creating a logger
    /// </summary>
    public static class GlobalLogger
    {
        // Reference to our shared logger for global use
        private static ILogger _logger;

        // Provider required by ASP.NET Core
        private static ILoggerProvider _provider;

        public static ILogger Logger
            => _logger ?? throw new InvalidOperationException("Logging has not been initialized.");

        public static ILoggerProvider Provider 
            => _provider ?? throw new InvalidOperationException("Logging has not been initialized.");

        /// <summary>
        /// Creates a configured logger to use within SesTest
        /// </summary>
        /// <remarks>Also caches a copy of the logger for later reference from elsewhere.</remarks>
        /// <param name="level">Desired logging level.</param>
        /// <param name="logFile">Destination log file (if any).</param>
        /// <returns>Instance of simple logger.</returns>
        public static ILogger CreateLogger(LogLevel level, FileInfo logFile = null)
        {
            const string consoleTemplate = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message}{NewLine}";

            var consoleConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(ConvertLevel(level))
                .WriteTo.ColoredConsole( outputTemplate: consoleTemplate);

            if (logFile != null)
            {
                consoleConfiguration = consoleConfiguration.WriteTo.File(logFile.FullName);
            }

            var consoleLogger = consoleConfiguration.CreateLogger();

            var serilogProvider = new SerilogLoggerProvider(consoleLogger);
            _provider = new UnpackingExceptionLogProvider(serilogProvider);
            _logger = _provider.CreateLogger(string.Empty);

            return _logger;
        }

        /// <summary>
        /// Convert from LogLevel to LogEventLevel for configuring our log
        /// </summary>
        /// <remarks>This should be available from Serilog but it's private.</remarks>
        /// <param name="level">Log level to convert.</param>
        /// <returns>Serilog equivalent.</returns>
        private static LogEventLevel ConvertLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;

                case LogLevel.Error:
                    return LogEventLevel.Error;

                case LogLevel.Warning:
                    return LogEventLevel.Warning;

                case LogLevel.Information:
                    return LogEventLevel.Information;

                case LogLevel.Debug:
                    return LogEventLevel.Debug;

                case LogLevel.None:
                default:
                    return LogEventLevel.Verbose;
            }
        }
    }
}
