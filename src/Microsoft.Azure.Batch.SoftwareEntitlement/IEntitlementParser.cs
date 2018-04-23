using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Responsible for parsing a string token and extracting a <see cref="NodeEntitlements"/>
    /// from it.
    /// </summary>
    public interface IEntitlementParser
    {
        /// <summary>
        /// Parses a token and builds a <see cref="NodeEntitlements"/> object, or an <see cref="Errorable"/>
        /// if any parsing or validation error occurs.
        /// </summary>
        /// <param name="token">A string token</param>
        /// <returns>
        /// An <see cref="Errorable{NodeEntitlements}"/> containing the result, or an
        /// <see cref="Errorable.Failure{NodeEntitlements}"/> if it failed to validate correctly.
        /// </returns>
        Errorable<NodeEntitlements> Parse(string token);
    }
}
