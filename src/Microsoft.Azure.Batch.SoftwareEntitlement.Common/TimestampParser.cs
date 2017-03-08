using System;
using System.Globalization;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Utility class for conversion of dates from strings
    /// </summary>
    /// <remarks>Wraps our specific conventions and knows about logging for diagnostics.</remarks>
    public class TimestampParser
    {
        // The expected format for timestamps
        public const string ExpectedFormat = "HH:mm d-MMM-yyyy";

        // Reference to our common logger
        private readonly ISimpleLogger _logger;

        /// <summary>
        /// Initialize a new instance of the <see cref="TimestampParser"/> class
        /// </summary>
        /// <param name="logger">Logger to use for any warnings.</param>
        public TimestampParser(ISimpleLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Try to parse a string into a DateTimeOffset
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <returns>A tuple containing either true, and a date; or false and a default date.</returns>
        public (bool successful, DateTimeOffset timestamp) TryParse(string value)
        {
            if (DateTimeOffset.TryParseExact(
                value, ExpectedFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
            {
                // Correctly parsed in the expected format
                return (true, result);
            }

            if (DateTimeOffset.TryParse(
                value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            {
                // Correctly parsed with detected format
                _logger.Warning(
                    "Timestamp {TimeStamp} was not in the expected format {Format}; using {DateTime:F}.", 
                    value, 
                    ExpectedFormat, 
                    result);
                return (true, result);
            }

            return (false, default(DateTimeOffset));
        }
    }
}
