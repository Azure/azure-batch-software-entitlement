using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Responsible for parsing a string token and extracting a <see cref="EntitlementTokenProperties"/>
    /// from it.
    /// </summary>
    public interface ITokenPropertyParser
    {
        /// <summary>
        /// Parses a token and builds a <see cref="EntitlementTokenProperties"/> object, or an <see cref="Errorable"/>
        /// if any parsing or validation error occurs.
        /// </summary>
        /// <param name="token">A string token</param>
        /// <returns>
        /// An <see cref="Result{TokenProperties,ErrorCollection}"/> containing the result, or an
        /// error if it failed to validate correctly.
        /// </returns>
        Result<EntitlementTokenProperties, ErrorCollection> Parse(string token);
    }
}
