using System;
using System.Globalization;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class CommandLineTokenPropertyProviderTests
    {
        // An empty set of command line arguments for testing
        private readonly GenerateCommandLine _commandLine = new GenerateCommandLine();

        public class Constructor : CommandLineTokenPropertyProviderTests
        {
            [Fact]
            public void GivenNullCommandLine_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new CommandLineEntitlementPropertyProvider(null));
                exception.ParamName.Should().Be("commandLine");
            }
        }

        public class VirtualMachineIdProperty : CommandLineTokenPropertyProviderTests
        {
            const string virtualMachineId = "virtualMachine";

            [Fact]
            public void WhenMissing_ValueIsNull()
            {
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.VirtualMachineId().AssertOk().Should().BeNull();
            }

            [Fact]
            public void WithId_HasExpectedValue()
            {
                _commandLine.VirtualMachineId = virtualMachineId;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.VirtualMachineId().AssertOk().Should().Be(virtualMachineId);
            }
        }

        public class NotBeforeProperty : CommandLineTokenPropertyProviderTests
        {
            private readonly string _validNotBefore =
                DateTimeOffset.Now.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);

            [Fact]
            public void WhenMissing_HasDefaultValue()
            {
                _commandLine.NotBefore = string.Empty;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotBefore().AssertOk();
            }

            [Fact]
            public void WhenInvalid_HasError()
            {
                _commandLine.NotBefore = "Not a timestamp";
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotBefore().AssertError().Should().Contain(e => e.Contains("NotBefore"));
            }

            [Fact]
            public void WhenValid_HasNoError()
            {
                _commandLine.NotBefore = _validNotBefore;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotBefore().AssertOk();
            }

            [Fact]
            public void WhenValid_HasExpectedValue()
            {
                _commandLine.NotBefore = _validNotBefore;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                provider.NotBefore().AssertOk().ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture)
                    .Should().Be(_validNotBefore);
            }
        }

        public class NotAfterProperty : CommandLineTokenPropertyProviderTests
        {
            private readonly string _validNotAfter =
                DateTimeOffset.Now.AddDays(7).ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);

            [Fact]
            public void WhenMissing_HasDefaultValue()
            {
                _commandLine.NotAfter = string.Empty;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotAfter().AssertOk();
            }

            [Fact]
            public void WhenInvalid_HasError()
            {
                _commandLine.NotAfter = "Not a timestamp";
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotAfter().AssertError().Should().Contain(e => e.Contains("NotAfter"));
            }

            [Fact]
            public void WhenValid_HasNoError()
            {
                _commandLine.NotAfter = _validNotAfter;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.NotAfter().AssertOk();
            }

            [Fact]
            public void WhenValid_HasExpectedValue()
            {
                _commandLine.NotAfter = _validNotAfter;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                provider.NotAfter().AssertOk().ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture)
                    .Should().Be(_validNotAfter);
            }
        }

        public class AudienceProperty : CommandLineTokenPropertyProviderTests
        {
            private readonly string _audience = "https://account.region.batch.azure.test";

            [Fact]
            public void WhenMissing_HasDefaultValue()
            {
                _commandLine.Audience = string.Empty;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.Audience().AssertOk().Should().Be(Claims.DefaultAudience);
            }

            [Fact]
            public void WhenValid_HasExpectedValue()
            {
                _commandLine.Audience = _audience;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.Audience().AssertOk().Should().Be(_audience);
            }
        }

        public class ApplicationsProperty : CommandLineTokenPropertyProviderTests
        {
            private const string ContosoHrApp = "contosoHR";
            private const string ContosoItApp = "contosoIT";

            [Fact]
            public void WhenEmpty_HasError()
            {
                _commandLine.ApplicationIds.Clear();
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.ApplicationIds().AssertError();
            }

            [Fact]
            public void WhenEmpty_HasExpectedError()
            {
                _commandLine.ApplicationIds.Clear();
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.ApplicationIds().AssertError().Should().Contain(e => e.Contains("application"));
            }

            [Fact]
            public void WhenSingleApplication_HasExpectedValue()
            {
                _commandLine.ApplicationIds = new[] { ContosoHrApp };
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.ApplicationIds().AssertOk().Should().BeEquivalentTo(_commandLine.ApplicationIds);
            }

            [Fact]
            public void WhenSingleApplication_HasNoError()
            {
                _commandLine.ApplicationIds = new[] { ContosoHrApp };
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.ApplicationIds().AssertOk();
            }

            [Fact]
            public void WhenMultipleApplications_HasExpectedValues()
            {
                _commandLine.ApplicationIds = new[] { ContosoHrApp, ContosoItApp };
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.ApplicationIds().AssertOk().Should().BeEquivalentTo(_commandLine.ApplicationIds);
            }
        }

        public class AddressesProperty : CommandLineTokenPropertyProviderTests
        {
            // sample IPAddresses to use for testing (sample addresses as per RFC5735)
            private readonly IPAddress _addressA = IPAddress.Parse("203.0.113.46");
            private readonly IPAddress _addressB = IPAddress.Parse("203.0.113.48");

            [Fact]
            public void WhenEmpty_HasDefaultValue()
            {
                _commandLine.Addresses.Clear();
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.IpAddresses().AssertOk();
            }

            [Fact]
            public void WhenEmpty_HasNonEmptyValue()
            {
                _commandLine.Addresses.Clear();
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.IpAddresses().AssertOk().Should().NotBeEmpty();
            }

            [Fact]
            public void WhenSingleAddress_HasExpectedValue()
            {
                _commandLine.Addresses = new[] { _addressA.ToString() };
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.IpAddresses().AssertOk().Should().HaveCount(1);
            }

            [Fact]
            public void WhenMultipleAddresses_HasExpectedValues()
            {
                _commandLine.Addresses = new[] { _addressA.ToString(), _addressB.ToString() };
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.IpAddresses().AssertOk().Should().HaveCount(2);
            }

            [Fact]
            public void WhenInvalidAddress_HasError()
            {
                _commandLine.Addresses.Add("Not.An.IP.Address");
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.IpAddresses().AssertError().Should().Contain(e => e.Contains("address"));
            }
        }

        public class IssuerProperty : CommandLineTokenPropertyProviderTests
        {
            private readonly string _issuer = "https://account.region.batch.azure.test";

            [Fact]
            public void WhenMissing_HasDefaultValue()
            {
                _commandLine.Issuer = string.Empty;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.Issuer().AssertOk().Should().Be(Claims.DefaultIssuer);
            }

            [Fact]
            public void WhenValid_HasExpectedValue()
            {
                _commandLine.Issuer = _issuer;
                var provider = new CommandLineEntitlementPropertyProvider(_commandLine);
                provider.Issuer().AssertOk().Should().Be(_issuer);
            }
        }
    }
}
