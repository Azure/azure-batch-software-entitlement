using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A log provider that unpacks all of the diagnostics that may be in an exception
    /// </summary>
    /// <remarks>Works as a wrapper around an existing provider.</remarks>
    public class UnpackingExceptionLogProvider : ILoggerProvider
    {
        // Reference to our wrapped provider
        private readonly ILoggerProvider _innerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnpackingExceptionLogProvider"/> class
        /// </summary>
        /// <param name="innerProvider">Existing provider we should wrap.</param>
        public UnpackingExceptionLogProvider(ILoggerProvider innerProvider)
        {
            _innerProvider = innerProvider;
        }

        public void Dispose()
        {
            _innerProvider.Dispose();
        }

        /// <summary>
        /// Creates a new <see cref="ILogger" /> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            var inner = _innerProvider.CreateLogger(categoryName);
            return new UnpackingExceptionLogger(inner);
        }
    }

    /// <summary>
    /// A logger that unpacks all of the diagnostics that may be in an exception
    /// </summary>
    public class UnpackingExceptionLogger : ILogger
    {
        // Reference to our nested logger
        private readonly ILogger _innerLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnpackingExceptionLogger"/> class
        /// </summary>
        /// <param name="innerLogger">Referencer to an underlying logger to use for exceptions.</param>
        public UnpackingExceptionLogger(ILogger innerLogger)
        {
            _innerLogger = innerLogger;
        }

        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create the actual message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _innerLogger.Log(logLevel, eventId, state, null, formatter);
            if (exception != null)
            {
                LogException(eventId, exception);
            }
        }

        /// <summary>
        /// Check to see whether logging is enabled at a specific level
        /// </summary>
        /// <param name="logLevel">Log level being queried.</param>
        /// <returns>True if messages at this log level are being emitted; false otherwise.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return _innerLogger.IsEnabled(logLevel);
        }

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return _innerLogger.BeginScope(state);
        }

        private void LogException(EventId eventId, Exception exception, string prefix = "")
        {
            const string indent = "    ";

            Log(
                LogLevel.Error,
                eventId,
                $"{prefix}{exception.Message} ({exception.GetType().Name})",
                null,
                (s, e) => s);
            if (exception.Data != null)
            {
                foreach (DictionaryEntry entry in exception.Data)
                {
                    Log(LogLevel.Information, eventId, $"{prefix}{entry.Key} = {entry.Value}", null, (s, e) => s);
                }
            }

            foreach (var line in AsLines(exception.StackTrace))
            {
                Log(LogLevel.Debug, eventId, prefix + line, null, (s, e) => s);
            }

            if (exception is AggregateException aggregate)
            {
                var newIndent = indent + prefix;
                foreach (var e in aggregate.InnerExceptions)
                {
                    LogException(eventId, e, newIndent);
                }
            }
            else if (exception.InnerException != null)
            {
                LogException(eventId, exception.InnerException, indent + prefix);
            }
        }

        // Convert a string containing mutiple lines into a series of lines
        private IEnumerable<string> AsLines(string content)
        {
            using (var lines = new StringReader(content))
            {
                string nextLine;
                while ((nextLine = lines.ReadLine()) != null)
                {
                    yield return nextLine;
                }
            }
        }
    }
}
