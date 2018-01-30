using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class NodeEntitlementsBuilderTests
    {
        // A valid set of command line arguments for testing
        private readonly GenerateCommandLine _commandLine = new GenerateCommandLine
        {
            VirtualMachineId = "Sample",
            Addresses = new List<string> { "127.0.0.1" },
            ApplicationIds = new List<string> { "contosoapp" },
            Audience = "https://account.region.batch.azure.test",
            Issuer= "https://account.region.batch.azure.test"
        };

        public class BuildMethod : NodeEntitlementsBuilderTests
        {
            [Fact]
            public void GivenNullCommandLine_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => NodeEntitlementsBuilder.Build(null));
                exception.ParamName.Should().Be("commandLine");
            }

            [Fact]
            public void GivenValidCommandLine_ReturnsNoErrors()
            {
                // If this test fails, verify that the command line specified by _commandLine 
                // (above) correctly specifies a valid token; If this constraint is violated, most 
                // all of the tests later in this file might fail with spurious errors.
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should()
                    .BeEmpty(because: "the command line represented by _commandLine should result in a valid token");
            }
        }

        public class VirtualMachineIdProperty : NodeEntitlementsBuilderTests
        {
            [Fact]
            public void WhenMissing_BuildReturnsValue()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenMissing_BuildReturnsEntitlementWithNoVirtualMachineId()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.VirtualMachineId.Should().BeNullOrEmpty();
            }

            [Fact]
            public void WithId_BuildReturnsNoErrorForVirtualMachineId()
            {
                _commandLine.VirtualMachineId = "virtualMachine";
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("virtual machine identifier"));
            }

            [Fact]
            public void WithId_PropertyHasExpectedValue()
            {
                const string virtualMachineId = "virtualMachine";
                _commandLine.VirtualMachineId = virtualMachineId;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.VirtualMachineId.Should().Be(virtualMachineId);
            }
        }

        public class NotBeforeProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _validNotBefore =
                DateTimeOffset.Now.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);

            [Fact]
            public void WhenMissing_BuildStillReturnsValue()
            {
                _commandLine.NotBefore = string.Empty;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenInvalid_BuildReturnsErrorForNotBefore()
            {
                _commandLine.NotBefore = "Not a timestamp";
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("NotBefore"));
            }

            [Fact]
            public void WhenValid_BuildReturnsNoErrorForNotBefore()
            {
                _commandLine.NotBefore = _validNotBefore;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("NotBefore"));
            }

            [Fact]
            public void WhenValid_PropertyHasExpectedValue()
            {
                _commandLine.NotBefore = _validNotBefore;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotBefore.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture)
                    .Should().Be(_validNotBefore);
            }
        }

        public class NotAfterProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _validNotAfter =
                DateTimeOffset.Now.AddDays(7).ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture);

            [Fact]
            public void WhenMissing_BuildStillReturnsValue()
            {
                _commandLine.NotAfter = string.Empty;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenInvalid_BuildReturnsErrorForNotAfter()
            {
                _commandLine.NotAfter = "Not a timestamp";
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("NotAfter"));
            }

            [Fact]
            public void WhenValid_BuildReturnsNoErrorForNotAfter()
            {
                _commandLine.NotAfter = _validNotAfter;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("NotAfter"));
            }

            [Fact]
            public void WhenValid_PropertyHasExpectedValue()
            {
                _commandLine.NotAfter = _validNotAfter;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotAfter.ToString(TimestampParser.ExpectedFormat, CultureInfo.InvariantCulture)
                    .Should().Be(_validNotAfter);
            }
        }

        public class AudienceProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _audience = "https://account.region.batch.azure.test";

            [Fact]
            public void WhenMissing_BuildReturnsDefaultValue()
            {
                _commandLine.Audience = string.Empty;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.Value.Audience.Should().Be(Claims.DefaultAudience);
            }

            [Fact]
            public void WhenValid_PropertyIsSet()
            {
                _commandLine.Audience = _audience;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.Value.Audience.Should().Be(_audience);
            }
        }

        public class ApplicationsProperty : NodeEntitlementsBuilderTests
        {
            private const string ContosoHrApp = "contosoHR";
            private const string ContosoItApp = "contosoIT";

            [Fact]
            public void WhenEmpty_BuildDoesNotReturnValue()
            {
                _commandLine.ApplicationIds.Clear();
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenEmpty_BuildReturnsErrorForApplication()
            {
                _commandLine.ApplicationIds.Clear();
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("application"));
            }

            [Fact]
            public void WhenSingleApplication_PropertyHasExpectedValue()
            {
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.Applications.Should().BeEquivalentTo(_commandLine.ApplicationIds);
            }

            [Fact]
            public void WhenSingleApplication_ReturnsNoErrorForApplication()
            {
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("application"));
            }

            [Fact]
            public void WhenMultipleApplications_PropertyHasExpectedValues()
            {
                _commandLine.ApplicationIds.Add(ContosoHrApp);
                _commandLine.ApplicationIds.Add(ContosoItApp);
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.Applications.Should().BeEquivalentTo(_commandLine.ApplicationIds);
            }
        }

        public class AddressesProperty : NodeEntitlementsBuilderTests
        {
            // sample IPAddresses to use for testing (sample addresses as per RFC5735)
            private readonly IPAddress _addressA = IPAddress.Parse("203.0.113.46");
            private readonly IPAddress _addressB = IPAddress.Parse("203.0.113.48");

            [Fact]
            public void WhenEmpty_StillBuildsResult()
            {
                _commandLine.Addresses.Clear();
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenEmpty_ReturnsDefaultAddresses()
            {
                _commandLine.Addresses.Clear();
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.IpAddresses.Should().NotBeEmpty();
            }

            [Fact]
            public void WhenSingleAddress_PropertyHasExpectedValue()
            {
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.IpAddresses.Should().HaveCount(1);
            }

            [Fact]
            public void WhenMultipleAddresses_PropertyHasExpectedValues()
            {
                _commandLine.Addresses.Add(_addressA.ToString());
                _commandLine.Addresses.Add(_addressB.ToString());
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.IpAddresses.Should().HaveCount(3);
            }

            [Fact]
            public void WhenInvalidAddress_ReturnsErrorForAddress()
            {
                _commandLine.Addresses.Add("Not.An.IP.Address");
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeFalse();
                entitlement.Errors.Should().Contain(e => e.Contains("address"));
            }
        }

        public class IssuerProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _issuer = "https://account.region.batch.azure.test";

            [Fact]
            public void WhenMissing_BuildReturnsDefaultValue()
            {
                _commandLine.Issuer = string.Empty;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.Value.Issuer.Should().Be(Claims.DefaultIssuer);
            }

            [Fact]
            public void WhenValid_PropertyHasExpectedValue()
            {
                _commandLine.Issuer = _issuer;
                var result = NodeEntitlementsBuilder.Build(_commandLine);
                result.Value.Issuer.Should().Be(_issuer);
            }
        }
    }
}
