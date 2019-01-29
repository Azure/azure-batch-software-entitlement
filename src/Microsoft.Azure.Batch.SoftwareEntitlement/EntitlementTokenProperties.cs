using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// The properties encoded in a token for a specific compute node
    /// </summary>
    public class EntitlementTokenProperties
    {
        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// The moment at which the token is issued
        /// </summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>
        /// The earliest moment at which the token is active
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// The latest moment at which the token is active
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// The set of applications that are entitled to run
        /// </summary>
        public ImmutableHashSet<string> Applications { get; }

        /// <summary>
        /// The IP addresses of the machine authorized to use this token
        /// </summary>
        public ImmutableHashSet<IPAddress> IpAddresses { get; }

        /// <summary>
        /// The unique identifier for this token
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// The audience for whom this token is intended
        /// </summary>
        public string Audience { get; }

        /// <summary>
        /// The issuer who hands out entitlement tokens
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitlementTokenProperties"/> class
        /// </summary>
        public EntitlementTokenProperties()
        {
            Applications = ImmutableHashSet<string>.Empty;
            IpAddresses = ImmutableHashSet<IPAddress>.Empty;
        }

        public static Result<EntitlementTokenProperties, ErrorCollection> Build(ITokenPropertyProvider provider) =>
            from notBefore in provider.NotBefore()
            join notAfter in provider.NotAfter() on true equals true
            join issuedAt in provider.IssuedAt() on true equals true
            join issuer in provider.Issuer() on true equals true
            join audience in provider.Audience() on true equals true
            join applicationIds in provider.ApplicationIds() on true equals true
            join ipAddresses in provider.IpAddresses() on true equals true
            join vmid in provider.VirtualMachineId() on true equals true
            join tokenId in provider.TokenId() on true equals true
            select new EntitlementTokenProperties()
                .FromInstant(notBefore)
                .UntilInstant(notAfter)
                .WithIssuedAt(issuedAt)
                .WithIssuer(issuer)
                .WithAudience(audience)
                .WithApplications(applicationIds)
                .WithIpAddresses(ipAddresses)
                .WithVirtualMachineId(vmid)
                .WithIdentifier(tokenId);

        /// <summary>
        /// Specify the virtual machine Id of the machine
        /// </summary>
        /// <param name="virtualMachineId">Virtual machine ID to include in the token.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithVirtualMachineId(string virtualMachineId)
        {
            return new EntitlementTokenProperties(this, virtualMachineId: Specify.As(virtualMachineId));
        }

        /// <summary>
        /// Specify the instant at which the token is issued
        /// </summary>
        /// <param name="issuedAt">Date the token is issued.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithIssuedAt(DateTimeOffset issuedAt)
        {
            return new EntitlementTokenProperties(this, issuedAt: Specify.As(issuedAt));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties FromInstant(DateTimeOffset notBefore)
        {
            return new EntitlementTokenProperties(this, notBefore: Specify.As(notBefore));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notAfter">Earliest instant of availability.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties UntilInstant(DateTimeOffset notAfter)
        {
            return new EntitlementTokenProperties(this, notAfter: Specify.As(notAfter));
        }

        /// <summary>
        /// Specify the entitled applications
        /// </summary>
        /// <param name="applications">Identifiers of the applications.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithApplications(params string[] applications)
        {
            if (applications == null)
            {
                throw new ArgumentNullException(nameof(applications));
            }

            return WithApplications((IEnumerable<string>)applications);
        }

        /// <summary>
        /// Specify the list of entitled applications
        /// </summary>
        /// <param name="applications">Identifiers of the applications.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithApplications(IEnumerable<string> applications)
        {
            if (applications == null)
            {
                throw new ArgumentNullException(nameof(applications));
            }

            applications = applications.Select(appId => appId.Trim());
            return new EntitlementTokenProperties(this, applications: ImmutableHashSet.CreateRange(applications));
        }

        /// <summary>
        /// Specify the IPAddresses of the entitled machine
        /// </summary>
        /// <param name="ipAddresses">IP Addresses of the machine to run the entitled application(s).</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithIpAddresses(params IPAddress[] ipAddresses)
        {
            if (ipAddresses == null)
            {
                throw new ArgumentNullException(nameof(ipAddresses));
            }

            return WithIpAddresses((IEnumerable<IPAddress>)ipAddresses);
        }

        /// <summary>
        /// Specify the list of IPAddresses of the entitled machine
        /// </summary>
        /// <param name="ipAddresses">IP Addresses of the machine to run the entitled application(s).</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithIpAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            if (ipAddresses == null)
            {
                throw new ArgumentNullException(nameof(ipAddresses));
            }

            return new EntitlementTokenProperties(this, addresses: ImmutableHashSet.CreateRange(ipAddresses));
        }

        /// <summary>
        /// Specify the token Id to use
        /// </summary>
        /// <param name="identifier">The token identifier to use for correlating activity.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithIdentifier(string identifier)
        {
            return new EntitlementTokenProperties(this, identifier: Specify.As(identifier));
        }

        /// <summary>
        /// Specify the audience to use in the token 
        /// </summary>
        /// <param name="audience">The audience for the generated token.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithAudience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("Expect to have an audience", nameof(audience));
            }

            return new EntitlementTokenProperties(this, audience: Specify.As(audience));
        }

        /// <summary>
        /// Specify the issuer to use in the token 
        /// </summary>
        /// <param name="issuer">The issuer for the generated token.</param>
        /// <returns>A new <see cref="EntitlementTokenProperties"/> instance.</returns>
        public EntitlementTokenProperties WithIssuer(string issuer)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Expect to have an issuer", nameof(issuer));
            }

            return new EntitlementTokenProperties(this, issuer: Specify.As(issuer));
        }

        /// <summary>
        /// Cloning constructor to initialize a new instance of the <see cref="EntitlementTokenProperties"/>
        /// class as a (near) copy of an existing one.
        /// </summary>
        /// <remarks>Specify any of the optional parameters to modify the clone from the original.</remarks>
        /// <param name="original">Original token properties to clone.</param>
        /// <param name="issuedAt">Optionally specify a new value for <see cref="IssuedAt"/></param>
        /// <param name="notBefore">Optionally specify a new value for <see cref="NotBefore"/>.</param>
        /// <param name="notAfter">Optionally specify a new value for <see cref="NotAfter"/>.</param>
        /// <param name="virtualMachineId">Optionally specify a new value for <see cref="VirtualMachineId"/>.</param>
        /// <param name="applications">The set of applications entitled to run.</param>
        /// <param name="identifier">Identifier to use for this token.</param>
        /// <param name="addresses">Addresses of the entitled machine.</param>
        /// <param name="audience">Audience for whom the token is intended.</param>
        /// <param name="issuer">Issuer identifier for the token.</param>
        private EntitlementTokenProperties(
            EntitlementTokenProperties original,
            Specifiable<DateTimeOffset> issuedAt = default,
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

            IssuedAt = issuedAt.OrDefault(original.IssuedAt);
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
