using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Serilog;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Implementation of logging that uses Serilog
    /// </summary>
    public class SerilogLogger : ISimpleLogger
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initialize a new instance of the <see cref="SerilogLogger"/> class.
        /// </summary>
        /// <param name="logger">Reference to a Serilog logger</param>
        public SerilogLogger(ILogger logger)
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
            _logger.Error(template, arguments);
        }

        /// <summary>
        /// Log a (non fatal) warning
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Warning(string template, params object[] arguments)
        {
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
