using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests.Fakes
{
    /// <summary>
    /// A logger that collects the messages logged for later asserting
    /// </summary>
    public class CollectingLogger : ILogger
    {
        // Our store of logged events
        private readonly List<LogEvent> _events = new List<LogEvent>();

        /// <summary>
        /// Gets the sequence of logged events we've seen
        /// </summary>
        public IEnumerable<LogEvent> Events => _events;

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logEvent = new LogEvent(logLevel, message);
            _events.Add(logEvent);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }

    public struct LogEvent
    {
        public LogLevel Level { get; }
        public string Message { get; }

        public LogEvent(LogLevel logLevel, string message) : this()
        {
            Level = logLevel;
            Message = message;
        }
    }
}
