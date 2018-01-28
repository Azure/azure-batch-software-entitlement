using System;
using System.Collections.Immutable;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// The software entitlements for a specific compute node
    /// </summary>
    public class NodeEntitlements
    {
        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// The moment at which the entitlement was created
        /// </summary>
        public DateTimeOffset Created { get; }

        /// <summary>
        /// The earliest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// The latest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// The set of applications that are entitled to run
        /// </summary>
        public ImmutableHashSet<string> Applications { get; }

        /// <summary>
        /// The IP addresses of the machine authorized to use this entitlement
        /// </summary>
        public ImmutableHashSet<IPAddress> IpAddresses { get; }

        /// <summary>
        /// The unique identifier for this entitlement
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// The audience for whom this entitlement is intended
        /// </summary>
        public string Audience { get; }

        /// <summary>
        /// The issuer who hands out entitlement tokens
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEntitlements"/> class
        /// </summary>
        public NodeEntitlements()
        {
            var now = DateTimeOffset.Now;

            VirtualMachineId = string.Empty;
            Created = now;
            NotBefore = now;
            NotAfter = now + TimeSpan.FromDays(7);
            Applications = ImmutableHashSet<string>.Empty;
            IpAddresses = ImmutableHashSet<IPAddress>.Empty;
            Audience = Claims.DefaultAudience;
            Issuer = Claims.DefaultIssuer;
        }

        /// <summary>
        /// Specify the virtual machine Id of the machine
        /// </summary>
        /// <param name="virtualMachineId">Virtual machine ID to include in the entitlement.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithVirtualMachineId(string virtualMachineId)
        {
            if (virtualMachineId == null)
            {
                throw new ArgumentNullException(nameof(virtualMachineId));
            }

            return new NodeEntitlements(this, virtualMachineId: Specify.As(virtualMachineId));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements FromInstant(DateTimeOffset notBefore)
        {
            return new NodeEntitlements(this, notBefore: Specify.As(notBefore));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notAfter">Earliest instant of availability.</param>
        /// <returns></returns>
        public NodeEntitlements UntilInstant(DateTimeOffset notAfter)
        {
            return new NodeEntitlements(this, notAfter: Specify.As(notAfter));
        }

        /// <summary>
        /// Add an application into the list of entitled applications
        /// </summary>
        /// <param name="application">Identifier of the application to add.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements AddApplication(string application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return new NodeEntitlements(this, applications: Applications.Add(application.Trim()));
        }

        /// <summary>
        /// Specify the IPAddress of the entitled machine
        /// </summary>
        /// <param name="address">IP Address of the machine to run the entitled application(s).</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements AddIpAddress(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return new NodeEntitlements(this, addresses: IpAddresses.Add(address));
        }

        /// <summary>
        /// Specify the entitlement Id to use
        /// </summary>
        /// <param name="identifier">The entitlement identifier to use for correlating activity.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("Expect to have an identifier", nameof(identifier));
            }

            return new NodeEntitlements(this, identifier: Specify.As(identifier));
        }

        /// <summary>
        /// Specify the audience to use in the token 
        /// </summary>
        /// <param name="audience">The audience for the generated token.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithAudience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("Expect to have an audience", nameof(audience));
            }

            return new NodeEntitlements(this, audience: Specify.As(audience));
        }

        /// <summary>
        /// Specify the issuer to use in the token 
        /// </summary>
        /// <param name="issuer">The issuer for the generated token.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIssuer(string issuer)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Expect to have an issuer", nameof(issuer));
            }

            return new NodeEntitlements(this, issuer: Specify.As(issuer));
        }

        /// <summary>
        /// Cloning constructor to initialize a new instance of the <see cref="NodeEntitlements"/>
        /// class as a (near) copy of an existing one.
        /// </summary>
        /// <remarks>Specify any of the optional parameters to modify the clone from the original.</remarks>
        /// <param name="original">Original entitlement to clone.</param>
        /// <param name="notBefore">Optionally specify a new value for <see cref="NotBefore"/>.</param>
        /// <param name="notAfter">Optionally specify a new value for <see cref="NotAfter"/>.</param>
        /// <param name="virtualMachineId">Optionally specify a new value for <see cref="VirtualMachineId"/>.</param>
        /// <param name="applications">The set of applications entitled to run.</param>
        /// <param name="identifier">Identifier to use for this entitlement.</param>
        /// <param name="addresses">Addresses of the entitled machine.</param>
        /// <param name="audience">Audience for whom the token is intended.</param>
        /// <param name="issuer">Issuer identifier for the token.</param>
        private NodeEntitlements(
            NodeEntitlements original,
            Specifiable<DateTimeOffset> notBefore = default,
            Specifiable<DateTimeOffset> notAfter = default,
            Specifiable<string> virtualMachineId = default,
            ImmutableHashSet<string> applications = null,
            Specifiable<string> identifier = default,
            ImmutableHashSet<IPAddress> addresses = null,
            Specifiable<string> audience = default,
            Specifiable<string> issuer = default)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            Created = original.Created;
            NotBefore = notBefore.OrDefault(original.NotBefore);
            NotAfter = notAfter.OrDefault(original.NotAfter);
            VirtualMachineId = virtualMachineId.OrDefault(original.VirtualMachineId);
            Applications = applications ?? original.Applications;
            Identifier = identifier.OrDefault(original.Identifier);
            IpAddresses = addresses ?? original.IpAddresses;
            Audience = audience.OrDefault(original.Audience);
            Issuer = issuer.OrDefault(original.Issuer);
        }
    }
}
