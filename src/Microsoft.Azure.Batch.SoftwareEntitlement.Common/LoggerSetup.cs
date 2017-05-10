using System;
using System.Collections.Generic;
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
    /// Configuration of logging
    /// </summary>
    public class LoggerSetup : IDisposable
    {
        // Serilog configuration
        private readonly LoggerConfiguration _configuration = new LoggerConfiguration().MinimumLevel.Debug();

        // Reference to a shared log provider
        private readonly Lazy<ILoggerProvider> _provider;

        // Reference to a root logger
        private readonly Lazy<ILogger> _logger;

        /// <summary>
        /// List of actions that were deferred until after our logging was fully configured
        /// </summary>
        /// <remarks>Used so we can log information about how logging was configured.</remarks>
        private readonly List<Action<ILogger>> _pending = new List<Action<ILogger>>();

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
        public LoggerSetup ToConsole(LogLevel level)
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
        public LoggerSetup ToFile(FileInfo logFile, LogLevel level)
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

        public LogLevel TryParse(string level, string purpose, LogLevel fallback)
        {
            if (string.IsNullOrEmpty(level))
            {
                // Default to fallback if not specified
                return fallback;
            }

            if (Enum.TryParse<LogLevel>(level, true, out var result))
            {
                // Successfully parsed the string
                return result;
            }

            _pending.Add(
                l => l.LogWarning("Failed to recognize {purpose} log level '{level}'; defaulting to {default}", purpose, level, fallback));

            return fallback;
        }

        public void Dispose()
        {
            LogPending();
        }

        // Write any log messages that have been held over until configuration is complete
        private void LogPending()
        {
            foreach (var p in _pending)
            {
                p(Logger);
            }
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
