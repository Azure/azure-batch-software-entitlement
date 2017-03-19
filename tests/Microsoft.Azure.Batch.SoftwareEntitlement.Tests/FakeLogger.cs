using System;
using System.Collections.Generic;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    /// <summary>
    /// A logger used when validating so that we can tell if there were any errors
    /// </summary>
    public class FakeLogger : ILogger
    {
        // Gets the counts of different log levels observed
        private readonly Dictionary<LogLevel, int> _counts =
            new Dictionary<LogLevel, int>();

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
        public FakeLogger()
        {
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
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state)
            => throw new NotImplementedException("Fake!");
    }
}
