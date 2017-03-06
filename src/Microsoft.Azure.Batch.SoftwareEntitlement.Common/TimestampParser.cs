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
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Option<DateTimeOffset> Parse(string value)
        {
            DateTimeOffset result;
            if (DateTimeOffset.TryParseExact(
                value, ExpectedFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            {
                // Correctly parsed in the expected format
                return Option<DateTimeOffset>.Some(result);
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
                return Option<DateTimeOffset>.Some(result);
            }

            return Option<DateTimeOffset>.None();
        }
    }
}
