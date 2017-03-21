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
        private readonly ILoggerProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnpackingExceptionLogProvider"/> class
        /// </summary>
        /// <param name="provider">Existing provider we should wrap.</param>
        public UnpackingExceptionLogProvider(ILoggerProvider provider)
        {
            _provider = provider;
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var nested = _provider.CreateLogger(categoryName);
            return new UnpackingExceptionLogger(nested);
        }
    }

    /// <summary>
    /// A logger that unpacks all of the diagnostics that may be in an exception
    /// </summary>
    public class UnpackingExceptionLogger : ILogger
    {
        // Reference to our nested logger
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnpackingExceptionLogger"/> class
        /// </summary>
        /// <param name="logger">Referencer to an underlying logger to use for exceptions.</param>
        public UnpackingExceptionLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, null, formatter);
            if (exception != null)
            {
                LogException(eventId, exception);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        private void LogException(EventId eventId, Exception exception, string prefix = "")
        {
            Log(LogLevel.Error, eventId, exception.Message, null, (s, e) => s);
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

            if (exception.InnerException != null)
            {
                LogException(eventId, exception.InnerException, prefix + "    ");
            }

            if (exception is AggregateException aggregate)
            {
                foreach (var e in aggregate.InnerExceptions)
                {
                    LogException(eventId, e, prefix + "    ");
                }
            }
        }

        public IEnumerable<string> AsLines(string content)
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
