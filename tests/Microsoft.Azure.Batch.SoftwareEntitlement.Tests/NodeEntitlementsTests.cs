using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class NodeEntitlementsTests
    {
        // An empty software entitlement to use for testing
        private readonly NodeEntitlements _emptyEntitlement = new NodeEntitlements();

        // A Times span representing NZDT
        private readonly TimeSpan _nzdt = new TimeSpan(+13, 0, 0);

        // An instant to use as the start for testing
        private readonly DateTimeOffset _start;

        // An instant to use as the finish for testing
        private readonly DateTimeOffset _finish;

        public NodeEntitlementsTests()
        {
            _start = new DateTimeOffset(2016, 2, 29, 16, 14, 12, _nzdt);
            _finish = new DateTimeOffset(2016, 3, 31, 16, 14, 12, _nzdt);
        }

        public class WithVirtualMachineIdMethod : NodeEntitlementsTests
        {
            [Fact]
            public void GivenNull_SetsIdToNull()
            {
                _emptyEntitlement.WithVirtualMachineId(null)
                    .VirtualMachineId.Should().Be(null);
            }

            [Fact]
            public void GivenVirtualMachineId_ConfiguresProperty()
            {
                const string vmid = "Sample";
                _emptyEntitlement.WithVirtualMachineId(vmid)
                    .VirtualMachineId.Should().Be(vmid);
            }
        }

        public class FromInstantMethod : NodeEntitlementsTests
        {
            [Fact]
            public void GivenStart_ConfiguresProperty()
            {
                _emptyEntitlement.FromInstant(_start)
                    .NotBefore.Should().Be(_start);
            }
        }

        public class UntilInstantMethod : NodeEntitlementsTests
        {
            [Fact]
            public void GivenFinish_ConfiguresProperty()
            {
                _emptyEntitlement.UntilInstant(_finish)
                    .NotAfter.Should().Be(_finish);
            }
        }

        public class WithApplicationsMethod : NodeEntitlementsTests
        {
            private const string Application = "contosoapp";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyEntitlement.WithApplications(null));
                exception.ParamName.Should().Be("applications");
            }

            [Fact]
            public void GivenApplicationId_AddsToConfiguration()
            {
                var entitlement = _emptyEntitlement.WithApplications(Application);
                entitlement.Applications.Should().HaveCount(1);
                entitlement.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenDuplicateApplicationId_DoesNotAddToConfiguration()
            {
                var entitlement = _emptyEntitlement
                    .WithApplications(Application, Application);
                entitlement.Applications.Should().HaveCount(1);
                entitlement.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenApplicationIdWithWhitespace_RemovesWhitespace()
            {
                var entitlement = _emptyEntitlement.WithApplications("  " + Application + "  ");
                entitlement.Applications.Should().HaveCount(1);
                entitlement.Applications.Should().Contain(Application.Trim());
            }
        }

        public class AddIpAddressMethod : NodeEntitlementsTests
        {
            // sample IPAddresses to use for testing (sample addresses as per RFC5735)
            private readonly IPAddress _addressA = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _addressB = IPAddress.Parse("203.0.113.44");

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyEntitlement.WithIpAddresses(null));
                exception.ParamName.Should().Be("ipAddresses");
            }

            [Fact]
            public void GivenIpAddress_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.WithIpAddresses(_addressA);
                entitlement.IpAddresses.Should().Contain(_addressA);
            }

            [Fact]
            public void GivenSecondIpAddress_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.WithIpAddresses(_addressB);
                entitlement.IpAddresses.Should().Contain(_addressB);
            }

            [Fact]
            public void GivenSecondIpAddress_RetainsFirst()
            {
                var entitlement = _emptyEntitlement
                    .WithIpAddresses(_addressA, _addressB);
                entitlement.IpAddresses.Should().Contain(_addressA);
            }
        }

        public class WithIdentifierMethod : NodeEntitlementsTests
        {
            // An identifier to use
            private readonly string _identifier = "an-identifier-for-an-entitlement";

            [Fact]
            public void GivenNull_SetsIdentifierToNull()
            {
                var entitlement = _emptyEntitlement.WithIdentifier(null);
                entitlement.Identifier.Should().BeNull();
            }

            [Fact]
            public void GivenBlank_SetsIdentifierToEmpty()
            {
                var entitlement = _emptyEntitlement.WithIdentifier(string.Empty);
                entitlement.Identifier.Should().BeEmpty();
            }

            [Fact]
            public void GivenIpAddress_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.WithIdentifier(_identifier);
                entitlement.Identifier.Should().Be(_identifier);
            }
        }

        public class WithAudienceMethod : NodeEntitlementsTests
        {
            // An audience to use
            private readonly string _audience = "http://batch.test.example.com/account";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyEntitlement.WithAudience(null));
                exception.ParamName.Should().Be("audience");
            }

            [Fact]
            public void GivenBlank_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyEntitlement.WithAudience(string.Empty));
                exception.ParamName.Should().Be("audience");
            }

            [Fact]
            public void GivenAudience_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.WithAudience(_audience);
                entitlement.Audience.Should().Be(_audience);
            }
        }

        public class Create : NodeEntitlementsTests
        {
            private readonly FakeEntitlementPropertyProvider _validProvider = FakeEntitlementPropertyProvider.CreateValid();

            [Fact]
            public void GivenValidReader_ReturnsNoErrors()
            {
                // If this test fails, verify that the command line specified by _commandLine 
                // (above) correctly specifies a valid token; If this constraint is violated, most 
                // all of the tests later in this file might fail with spurious errors.
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Errors.Should()
                    .BeEmpty(because: $"the command line represented by {nameof(_validProvider)} should result in a valid token");
            }

            [Fact]
            public void GivenValidReader_ApplicationIdsAreSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.Applications.Should().BeEquivalentTo(_validProvider.ApplicationIds.Value);
            }

            [Fact]
            public void GivenValidReader_AudienceIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.Audience.Should().Be(_validProvider.Audience.Value);
            }

            [Fact]
            public void GivenValidReader_IdentifierIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.Identifier.Should().Be(_validProvider.EntitlementId.Value);
            }

            [Fact]
            public void GivenValidReader_IpAddressesAreSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.IpAddresses.Should().BeEquivalentTo(_validProvider.IpAddresses.Value);
            }

            [Fact]
            public void GivenValidReader_IssuedAtIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.IssuedAt.Should().Be(_validProvider.IssuedAt.Value);
            }

            [Fact]
            public void GivenValidReader_IssuerIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.Issuer.Should().Be(_validProvider.Issuer.Value);
            }

            [Fact]
            public void GivenValidReader_NotAfterIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.NotAfter.Should().Be(_validProvider.NotAfter.Value);
            }

            [Fact]
            public void GivenValidReader_NotBeforeIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.NotBefore.Should().Be(_validProvider.NotBefore.Value);
            }

            [Fact]
            public void GivenValidReader_VirtualMachineIdIsSet()
            {
                var entitlement = NodeEntitlements.Build(_validProvider);
                entitlement.Value.VirtualMachineId.Should().Be(_validProvider.VirtualMachineId.Value);
            }
        }
    }
}
