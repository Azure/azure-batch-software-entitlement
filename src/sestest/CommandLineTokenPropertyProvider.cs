using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// A factory object that tries to create a <see cref="EntitlementTokenProperties"/> instance when given 
    /// the <see cref="GenerateCommandLine"/> specified by the user.
    /// </summary>
    public class CommandLineEntitlementPropertyProvider : ITokenPropertyProvider
    {
        // Reference to the generate command line we wrap
        private readonly GenerateCommandLine _commandLine;

        // Reference to a parser to use for timestamps
        private readonly TimestampParser _timestampParser = new TimestampParser();

        // A steady reference for "now"
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        private readonly string _tokenId = $"entitlement-{Guid.NewGuid():D}";

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCommandLine"/> class
        /// </summary>
        /// <param name="commandLine">Options provided on the command line.</param>
        public CommandLineEntitlementPropertyProvider(GenerateCommandLine commandLine)
        {
            _commandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
        }

        public Errorable<DateTimeOffset> IssuedAt()
            => Errorable.Success(_now);

        public Errorable<DateTimeOffset> NotBefore()
        {
            if (string.IsNullOrEmpty(_commandLine.NotBefore))
            {
                // If the user does not specify a start instant for the token, we default to 'now'
                return Errorable.Success(_now);
            }

            return _timestampParser.TryParse(_commandLine.NotBefore, "NotBefore");
        }

        public Errorable<DateTimeOffset> NotAfter()
        {
            if (string.IsNullOrEmpty(_commandLine.NotAfter))
            {
                // If the user does not specify an expiry for the token, we default to 7days from 'now'
                return Errorable.Success(_now + TimeSpan.FromDays(7));
            }

            return _timestampParser.TryParse(_commandLine.NotAfter, "NotAfter");
        }

        public Errorable<string> Audience()
        {
            if (string.IsNullOrEmpty(_commandLine.Audience))
            {
                // if the user does not specify an audience, we use a default value to "self-sign"
                return Errorable.Success(Claims.DefaultAudience);
            }

            return Errorable.Success(_commandLine.Audience);
        }

        public Errorable<string> Issuer()
        {
            if (string.IsNullOrEmpty(_commandLine.Issuer))
            {
                // if the user does not specify an issuer, we use a default value to "self-sign"
                return Errorable.Success(Claims.DefaultIssuer);
            }

            return Errorable.Success(_commandLine.Issuer);
        }

        public Errorable<IEnumerable<IPAddress>> IpAddresses()
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

            return result.Reduce();
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

        public Errorable<IEnumerable<string>> ApplicationIds()
        {
            if (_commandLine.ApplicationIds == null || !_commandLine.ApplicationIds.Any())
            {
                return Errorable.Failure<IEnumerable<string>>("No applications specified.");
            }

            var apps = _commandLine.ApplicationIds.Select(app => app.Trim());
            return Errorable.Success(apps);
        }

        public Errorable<string> VirtualMachineId()
            => Errorable.Success(_commandLine.VirtualMachineId);

        public Errorable<string> TokenId()
            => Errorable.Success(_tokenId);
    }
}
