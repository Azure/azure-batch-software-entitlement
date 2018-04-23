using System;
using System.Linq;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Verifies whether a software entitlement request is valid or not
    /// </summary>
    public class EntitlementVerifier
    {
        private readonly IEntitlementParser _entitlementParser;

        /// <summary>
        /// Constructs a new <see cref="EntitlementVerifier"/>
        /// </summary>
        /// <param name="tokenParser">Extracts identity information from a string token</param>
        public EntitlementVerifier(IEntitlementParser entitlementParser)
        {
            _entitlementParser = entitlementParser ?? throw new ArgumentNullException(nameof(entitlementParser));
        }

        /// <summary>
        /// Verifies an entitlement request against the specified token.
        /// </summary>
        /// <param name="request">The entitlement request</param>
        /// <param name="token">The token string containing the entitlements</param>
        /// <returns>An <see cref="Errorable{NodeEntitlements}"/> containing the validated
        /// <see cref="NodeEntitlements"/> if validation was successful, or one or more
        /// validation errors otherwise.</returns>
        public Errorable<NodeEntitlements> Verify(
            EntitlementVerificationRequest request,
            string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return _entitlementParser
                .Parse(token)
                .Bind(e => Verify(request, e));
        }

        private static Errorable<NodeEntitlements> Verify(
            EntitlementVerificationRequest request,
            NodeEntitlements entitlement)
        {
            if (!entitlement.Applications.Any(a => string.Equals(a, request.ApplicationId, StringComparison.OrdinalIgnoreCase)))
            {
                return Errorable.Failure<NodeEntitlements>($"Token does not grant entitlement for {request.ApplicationId}");
            }

            if (!entitlement.IpAddresses.Any(addr => addr.Equals(request.IpAddress)))
            {
                return Errorable.Failure<NodeEntitlements>($"Token does not grant entitlement for {request.IpAddress}");
            }

            return Errorable.Success(entitlement);
        }
    }
}
