using System;
using System.Linq;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Verifies whether a software entitlement request is valid or not
    /// </summary>
    public class TokenVerifier
    {
        private readonly ITokenPropertyParser _tokenPropertyParser;

        /// <summary>
        /// Constructs a new <see cref="TokenVerifier"/>
        /// </summary>
        /// <param name="tokenPropertyParser">Extracts identity information from a string token</param>
        public TokenVerifier(ITokenPropertyParser tokenPropertyParser)
        {
            _tokenPropertyParser = tokenPropertyParser ?? throw new ArgumentNullException(nameof(tokenPropertyParser));
        }

        /// <summary>
        /// Verifies an entitlement request against the specified token.
        /// </summary>
        /// <param name="request">The entitlement request</param>
        /// <param name="token">The token string containing the entitlements</param>
        /// <returns>An <see cref="Result{NodeEntitlements,ErrorSet}"/> containing the validated
        /// <see cref="EntitlementTokenProperties"/> if validation was successful, or one or more
        /// validation errors otherwise.</returns>
        public Result<EntitlementTokenProperties,ErrorSet> Verify(
            TokenVerificationRequest request,
            string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return
                from parsedProps in _tokenPropertyParser.Parse(token)
                from verifiedProps in Verify(request, parsedProps)
                select verifiedProps;
        }

        private static Result<EntitlementTokenProperties, ErrorSet> Verify(
            TokenVerificationRequest request,
            EntitlementTokenProperties tokenProperties)
        {
            if (!tokenProperties.Applications.Any(a => string.Equals(a, request.ApplicationId, StringComparison.OrdinalIgnoreCase)))
            {
                return ErrorSet.Create($"Token does not grant entitlement for {request.ApplicationId}");
            }

            if (!tokenProperties.IpAddresses.Any(addr => addr.Equals(request.IpAddress)))
            {
                return ErrorSet.Create($"Token does not grant entitlement for {request.IpAddress}");
            }

            return tokenProperties;
        }
    }
}
