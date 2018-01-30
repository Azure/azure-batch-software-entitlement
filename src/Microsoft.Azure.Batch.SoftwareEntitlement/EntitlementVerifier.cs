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
        private readonly IHostVerifier _hostVerifier;

        /// <summary>
        /// Constructs a new <see cref="EntitlementVerifier"/>
        /// </summary>
        /// <param name="entitlementParser">Extracts identity information from a string token</param>
        /// <param name="hostVerifier">Verifies whether a host is permitted for a given entitlement</param>
        public EntitlementVerifier(
		    IEntitlementParser entitlementParser,
            IHostVerifier hostVerifier)
        {
            _entitlementParser = entitlementParser ?? throw new ArgumentNullException(nameof(entitlementParser));
            _hostVerifier = hostVerifier ?? throw new ArgumentNullException(nameof(hostVerifier));
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

        private Errorable<NodeEntitlements> Verify(
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

            // There should always be a CPU core count in the token, even though the
            // particular version of the API we're using may not require it to be
            // checked.
            if (!entitlement.CpuCoreCount.HasValue)
            {
                return Errorable.Failure<NodeEntitlements>("Token does not grant entitlement for any CPU cores");
            }

            // The presence of a CPU core count in the request indicates that we need to validate
            // it against the maximum permitted number in the entitlement.
            // Checking whether we're *supposed* to have a value provided in the request is
            // beyond the scope of this method.
            if (request.CpuCoreCount.HasValue &&
                request.CpuCoreCount.Value > entitlement.CpuCoreCount.Value)
            {
                return Errorable.Failure<NodeEntitlements>(
                    $"Token does not grant entitlement for {request.CpuCoreCount.Value} CPU cores");
            }

            // The presence of a hostId value in the request indicates that we need to check it.
            if (request.HostId != null &&
                !_hostVerifier.Verify(entitlement, request.HostId))
            {
                return Errorable.Failure<NodeEntitlements>(
                    $"Host {request.HostId} is not allowed for entitlement {entitlement.Identifier}");
            }

            return Errorable.Success(entitlement);
        }
    }
}
