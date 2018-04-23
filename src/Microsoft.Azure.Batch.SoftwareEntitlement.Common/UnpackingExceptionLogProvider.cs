using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A log provider that unpacks all of the diagnostics that may be in an exception
    /// </summary>
    /// <remarks>Works as a wrapper around an existing provider.</remarks>
    public sealed class UnpackingExceptionLogProvider : ILoggerProvider
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
        /// <returns>New logger for the specified category.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            var inner = _innerProvider.CreateLogger(categoryName);
            return new UnpackingExceptionLogger(inner);
        }
    }
}
