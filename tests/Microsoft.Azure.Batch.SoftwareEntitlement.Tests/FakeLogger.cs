using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public bool HasErrors => ReadCount(LogLevel.Error) > 0;

        /// <summary>
        /// Gets a value indicating whether this logger has logged any warnings
        /// </summary>
        public bool HasWarnings => ReadCount(LogLevel.Warning)> 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeLogger"/> class
        /// </summary>
        public FakeLogger()
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            WriteCount(logLevel, ReadCount(logLevel) + 1);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        private int ReadCount(LogLevel logLevel)
        {
            if (_counts.TryGetValue(logLevel, out var count))
            {
                return count;
            }

            return 0;
        }

        private void WriteCount(LogLevel logLevel, int count)
        {
            _counts[logLevel] = count;
        }

        [SuppressMessage(
            "General",
            "RCS1079:Throwing of new NotImplementedException.",
            Justification = "BeginScope() is not used by our tests")]
        public IDisposable BeginScope<TState>(TState state)
            => throw new NotImplementedException("Fake!");
    }
}
