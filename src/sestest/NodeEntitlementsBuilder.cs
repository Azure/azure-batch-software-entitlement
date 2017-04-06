using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

        // Reference to a store in which we can search for certificates
        private readonly CertificateStore _certificateStore = new CertificateStore();

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
            var entitlement = new NodeEntitlements();
            var errors = new List<string>();

            // readConfiguration - function to read the configuration value
            // applyConfiguration - function to modify our configuration with the value read
            void Configure<V>(Func<Errorable<V>> readConfiguration, Func<V, NodeEntitlements> applyConfiguration)
            {
                readConfiguration().Match(
                    whenSuccessful: value => entitlement = applyConfiguration(value),
                    whenFailure: e => errors.AddRange(e));
            }

            // readConfiguration - function to read all the configuration values
            // applyConfiguration - function to modify our configuration with each value read
            void ConfigureAll<V>(
                Func<IEnumerable<Errorable<V>>> readConfiguration,
                Func<V, NodeEntitlements> applyConfiguration)
            {
                foreach (var configuration in readConfiguration())
                {
                    configuration.Match(
                            whenSuccessful: value => entitlement = applyConfiguration(value),
                            whenFailure: e => errors.AddRange(e));
                }
            }

            Configure(VirtualMachineId, url => entitlement.WithVirtualMachineId(url));
            Configure(NotBefore, notBefore => entitlement.FromInstant(notBefore));
            Configure(NotAfter, notAfter => entitlement.UntilInstant(notAfter));
            ConfigureAll(Application, app => entitlement.AddApplication(app));
            ConfigureAll(Addresses, address => entitlement.AddIpAddress(address));

            if (errors.Any())
            {
                return Errorable.Failure<NodeEntitlements>(errors);
            }

            return Errorable.Success(entitlement);
        }

        private Errorable<string> VirtualMachineId()
        {
            if (string.IsNullOrEmpty(_commandLine.VirtualMachineId))
            {
                return Errorable.Failure<string>("No virtual machine identifier specified.");
            }

            return Errorable.Success(_commandLine.VirtualMachineId);
        }

        private Errorable<DateTimeOffset> NotBefore()
        {
            if (string.IsNullOrEmpty(_commandLine.NotBefore))
            {
                // If the user does not specify a start instant for the token, we default to 'now'
                return Errorable.Success(DateTimeOffset.Now);
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

        private IEnumerable<Errorable<IPAddress>> Addresses()
        {
            if (_commandLine.Addresses == null || !_commandLine.Addresses.Any())
            {
                yield return Errorable.Failure<IPAddress>("No IP Addresses specified.");
                yield break;
            }

            foreach (var address in _commandLine.Addresses)
            {
                if (!IPAddress.TryParse(address, out var ip))
                {
                    yield return Errorable.Failure<IPAddress>($"IP address '{address}' not in expected format (IPv4 and IPv6 supported).");
                }
                else
                {
                    yield return Errorable.Success(ip);
                }
            }
        }

        private IEnumerable<Errorable<string>> Application()
        {
            var apps = _commandLine.ApplicationIds.ToList();
            if (!_commandLine.ApplicationIds.Any())
            {
                yield return Errorable.Failure<string>("No applications specified.");
            }

            foreach (var app in apps)
            {
                yield return Errorable.Success(app);
            }
        }
    }
}
