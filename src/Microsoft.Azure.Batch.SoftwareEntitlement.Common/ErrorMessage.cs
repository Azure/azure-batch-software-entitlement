using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// An error message returned as part of the response to an unsuccessful request for a
    /// software entitlement
    /// </summary>
    public class ErrorMessage
    {
        [JsonProperty(PropertyName = "lang")]
        public string Language { get; set; } = "en-us";

        /// <summary>
        /// The actual message for human consumption
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Intializes a new instance of <see cref="ErrorMessage"/> with a specific message
        /// </summary>
        /// <param name="value">Human readable error message.</param>
        public ErrorMessage(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Intializes a new instance of <see cref="ErrorMessage"/> with a specific message and language
        /// </summary>
        /// <param name="language">Language of the message.</param>
        /// <param name="value">Human readable error message.</param>
        public ErrorMessage(string language, string value)
        {
            Language = language;
            Value = value;
        }
    }
}