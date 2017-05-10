using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Utility class to configure logging for use
    /// </summary>
    public class LoggerSetup
    {
        // Serilog configuration
        private readonly LoggerConfiguration _configuration = new LoggerConfiguration().MinimumLevel.Debug();

        // Reference to a shared log provider
        private readonly Lazy<ILoggerProvider> _provider;

        // Reference to a root logger
        private readonly Lazy<ILogger> _logger;

        /// <summary>
        /// Gets a reference to our root logger
        /// </summary>
        public ILogger Logger => _logger.Value;

        /// <summary>
        /// Gets a reference to our shared log provider
        /// </summary>
        /// <remarks>Used by ASP.NET Core.</remarks>
        public ILoggerProvider Provider => _provider.Value;

        public LoggerSetup()
        {
            _provider = new Lazy<ILoggerProvider>(CreateProvider, LazyThreadSafetyMode.ExecutionAndPublication);
            _logger = new Lazy<ILogger>(CreateLogger, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Configure logging to write to the console
        /// </summary>
        /// <param name="level">Desired logging level</param>
        public LoggerSetup SendToConsole(LogLevel level)
        {
            const string consoleTemplate = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message}{NewLine}";

            var eventLevel = ConvertLevel(level);
            _configuration.WriteTo.ColoredConsole(eventLevel, outputTemplate: consoleTemplate);

            return this;
        }

        /// <summary>
        /// Configure logging to write to a file
        /// </summary>
        /// <param name="logFile">Destination log file.</param>
        /// <param name="level">Desired logging level</param>
        public LoggerSetup SendToFile(FileInfo logFile, LogLevel level)
        {
            const string fileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}";

            if (logFile == null)
            {
                throw new ArgumentNullException(nameof(logFile));
            }

            var eventLevel = ConvertLevel(level);
            _configuration.WriteTo.File(
                logFile.FullName,
                eventLevel,
                outputTemplate: fileTemplate,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(10));

            return this;
        }

        private ILoggerProvider CreateProvider()
        {
            var logger = _configuration.CreateLogger();

            var serilogProvider = new SerilogLoggerProvider(logger);
            return new UnpackingExceptionLogProvider(serilogProvider);
        }

        private ILogger CreateLogger()
        {
            return Provider.CreateLogger(string.Empty);
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

                default:
                    return LogEventLevel.Verbose;
            }
        }
    }
}


