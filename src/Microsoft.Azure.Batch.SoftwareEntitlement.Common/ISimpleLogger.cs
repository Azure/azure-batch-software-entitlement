namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A very simple abstraction for logging
    /// </summary>
    /// <remarks>We use this to (a) isolate our actual logger from the rest of the code; and 
    /// (b) so that we can track message generation (e.g. <see cref="MonitoringLogger"/>).
    /// </remarks>
    public interface ISimpleLogger
    {
        /// <summary>
        /// Log a (fatal) error
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        void Error(string template, params object[] arguments);

        /// <summary>
        /// Log a (non fatal) warning
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        void Warning(string template, params object[] arguments);

        /// <summary>
        /// Log a information
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        void Information(string template, params object[] arguments);

        /// <summary>
        /// Log debug details
        /// </summary>
        /// <param name="template">Template to use for the log message.</param>
        /// <param name="arguments">Arguments to use to fill out the template.</param>
        void Debug(string template, params object[] arguments);
    }
}
