namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    /// <summary>
    /// Logger implementation that discards all messages
    /// </summary>
    public class NullLogger : ISimpleLogger
    {
        /// <summary>
        /// Discard a (fatal) error
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Error(string template, params object[] arguments)
        {
            // Nothing
        }

        /// <summary>
        /// Discard a (non fatal) warning
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Warning(string template, params object[] arguments)
        {
            // Nothing
        }

        /// <summary>
        /// Discard information
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Information(string template, params object[] arguments)
        {
            // Nothing
        }

        /// <summary>
        /// Discard debug details
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        public void Debug(string template, params object[] arguments)
        {
            // Nothing
        }
    }
}
