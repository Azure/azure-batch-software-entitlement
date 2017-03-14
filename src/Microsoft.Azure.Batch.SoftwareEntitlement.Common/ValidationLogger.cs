using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A logger used when validating so that we can tell if there were any errors
    /// </summary>
    public class ValidationLogger : ILogger
    {
        // Gets the counts of different log levels observed
        private readonly Dictionary<LogLevel, int> _counts =
            new Dictionary<LogLevel, int>();

        // Reference to the actual logger that we wrap
        private readonly ILogger _logger;

        /// <summary>
        /// Gets a value indicating whether this logger has logged any errors
        /// </summary>
        public bool HasErrors => _counts[LogLevel.Error] > 0;

        /// <summary>
        /// Gets a value indicating whether this logger has logged any warnings
        /// </summary>
        public bool HasWarnings => _counts[LogLevel.Warning] > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationLogger"/> class
        /// </summary>
        /// <param name="logger">Original logger that we should wrap.</param>
        public ValidationLogger(ILogger logger)
        {
            _logger = logger;

            _counts[LogLevel.None] = 0;
            _counts[LogLevel.Critical] = 0;
            _counts[LogLevel.Error] = 0;
            _counts[LogLevel.Warning] = 0;
            _counts[LogLevel.Information] = 0;
            _counts[LogLevel.Debug] = 0;
            _counts[LogLevel.Trace] = 0;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _counts[logLevel]++;
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }
    }
}
