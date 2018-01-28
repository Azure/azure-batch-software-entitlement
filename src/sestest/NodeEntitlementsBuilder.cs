using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// A factory object that tries to create a <see cref="NodeEntitlements"/> instance when given 
    /// the <see cref="GenerateCommandLine"/> specified by the user.
    /// </summary>
    public class NodeEntitlementsBuilder
    {
        // Reference to the generate command line we wrap
        private readonly GenerateCommandLine _commandLine;

        // Reference to a parser to use for timestamps
        private readonly TimestampParser _timestampParser = new TimestampParser();

        // A steady reference for "now"
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        /// <summary>
        /// Build an instance of <see cref="NodeEntitlements"/> from the information supplied on the 
        /// command line by the user
        /// </summary>
        /// <param name="commandLine">Command line parameters supplied by the user.</param>
        /// <returns>Either a usable (and completely valid) <see cref="NodeEntitlements"/> or a set 
        /// of errors.</returns>
        public static Errorable<NodeEntitlements> Build(GenerateCommandLine commandLine)
        {
            var builder = new NodeEntitlementsBuilder(commandLine);
            return builder.Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCommandLine"/> class
        /// </summary>
        /// <param name="commandLine">Options provided on the command line.</param>
        private NodeEntitlementsBuilder(GenerateCommandLine commandLine)
        {
            _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
        }

        /// <summary>
        /// Build an instance of <see cref="NodeEntitlements"/> from the information supplied on the 
        /// command line by the user
        /// </summary>
        /// <returns>Either a usable (and completely valid) <see cref="NodeEntitlements"/> or a set 
        /// of errors.</returns>
        private Errorable<NodeEntitlements> Build()
        {
            var result = Errorable.Success(new NodeEntitlements())
                .Configure(VirtualMachineId(), (e, url) => e.WithVirtualMachineId(url))
                .Configure(NotBefore(), (e, notBefore) => e.FromInstant(notBefore))
                .Configure(NotAfter(), (e, notAfter) => e.UntilInstant(notAfter))
                .Configure(Audience(), (e, audience) => e.WithAudience(audience))
                .Configure(Issuer(), (e, issuer) => e.WithIssuer(issuer))
                .ConfigureAll(Addresses(), (e, address) => e.AddIpAddress(address))
                .ConfigureAll(Applications(), (e, app) => e.AddApplication(app));

            return result;
        }

        private Errorable<string> VirtualMachineId()
        {
            // VirtualMachineId is not allowed to be set to null, but string.Empty is valid
            // (that's the value if otherwise unspecified).
            return Errorable.Success(_commandLine.VirtualMachineId ?? string.Empty);
        }

        private Errorable<DateTimeOffset> NotBefore()
        {
            if (string.IsNullOrEmpty(_commandLine.NotBefore))
            {
                // If the user does not specify a start instant for the token, we default to 'now'
                return Errorable.Success(_now);
            }

            return _timestampParser.TryParse(_commandLine.NotBefore, "NotBefore");
        }

        private Errorable<DateTimeOffset> NotAfter()
        {
            if (string.IsNullOrEmpty(_commandLine.NotAfter))
            {
                // If the user does not specify an expiry for the token, we default to 7days from 'now'
                return Errorable.Success(_now + TimeSpan.FromDays(7));
            }

            return _timestampParser.TryParse(_commandLine.NotAfter, "NotAfter");
        }

        private Errorable<string> Audience()
        {
            if (string.IsNullOrEmpty(_commandLine.Audience))
            {
                // if the audience does not specify an audience, we use a default value to "self-sign"
                return Errorable.Success(Claims.DefaultAudience);
            }

            return Errorable.Success(_commandLine.Audience);
        }

        private Errorable<string> Issuer()
        {
            if (string.IsNullOrEmpty(_commandLine.Issuer))
            {
                // if the audience does not specify an issuer, we use a default value to "self-sign"
                return Errorable.Success(Claims.DefaultIssuer);
            }

            return Errorable.Success(_commandLine.Issuer);
        }

        private IEnumerable<Errorable<IPAddress>> Addresses()
        {
            var result = new List<Errorable<IPAddress>>();
            if (_commandLine.Addresses != null)
            {
                foreach (var address in _commandLine.Addresses)
                {
                    result.Add(TryParseIPAddress(address));
                }
            }

            if (!result.Any())
            {
                result.AddRange(ListMachineIpAddresses());
            }

            return result;
        }

        private static IEnumerable<Errorable<IPAddress>> ListMachineIpAddresses()
        {
            // No IP addresses specified by the user, default to using all from the current machine
            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = i.GetIPProperties();
                var unicast = properties.UnicastAddresses;
                if (unicast != null)
                {
                    foreach (var info in unicast)
                    {
                        // Strip out the ScopeId for any local IPv6 addresses
                        // (Can't just assign 0 to ScopeId, that doesn't work)
                        var bytes = info.Address.GetAddressBytes();
                        var ip = new IPAddress(bytes);

                        yield return Errorable.Success(ip);
                    }
                }
            }
        }

        private static Errorable<IPAddress> TryParseIPAddress(string address)
        {
            if (IPAddress.TryParse(address, out var ip))
            {
                return Errorable.Success(ip);
            }

            return Errorable.Failure<IPAddress>($"IP address '{address}' is not in an expected format (IPv4 and IPv6 supported).");
        }

        private IEnumerable<Errorable<string>> Applications()
        {
            if (_commandLine.ApplicationIds == null || !_commandLine.ApplicationIds.Any())
            {
                yield return Errorable.Failure<string>("No applications specified.");
                yield break;
            }

            var apps = _commandLine.ApplicationIds.ToList();
            foreach (var app in apps)
            {
                yield return Errorable.Success(app.Trim());
            }
        }
    }
}
