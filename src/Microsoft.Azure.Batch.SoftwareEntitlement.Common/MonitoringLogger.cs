using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A simple logging wrapper that keeps track of whether certain levels of message have been logged.
    /// </summary>
    public class MonitoringLogger : ISimpleLogger
    {
        // Reference to the actual logger that we wrap
        private readonly ISimpleLogger _logger;

        // Count of errors we've logged
        private int _errorCount;

        // Count of warnings we've logged
        private int _warningCount;

        /// <summary>
        /// Gets a value indicating whether this logger has logged any errors
        /// </summary>
        public bool HasErrors => _errorCount > 0;

        /// <summary>
        /// Gets a value indicating whether this logger has logged any warnings
        /// </summary>
        public bool HasWarnings => _warningCount > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringLogger"/> class
        /// </summary>
        /// <param name="logger">Original logger that we should wrap.</param>
        public MonitoringLogger(ISimpleLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Log a (fatal) error
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Error(string template, params object[] arguments)
        {
            _errorCount++;
            _logger.Error(template, arguments);
        }

        /// <summary>
        /// Log a (non fatal) warning
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Warning(string template, params object[] arguments)
        {
            _warningCount++;
            _logger.Warning(template, arguments);
        }

        /// <summary>
        /// Log a information
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Information(string template, params object[] arguments)
        {
            _logger.Information(template, arguments);
        }

        /// <summary>
        /// Log debug details
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Debug(string template, params object[] arguments)
        {
            _logger.Debug(template, arguments);
        }
    }
}
