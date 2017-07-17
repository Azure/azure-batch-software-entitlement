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
            public void GivenNull_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyEntitlement.WithVirtualMachineId(null));
                exception.ParamName.Should().Be("virtualMachineId");
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

        public class AddApplicationMethod : NodeEntitlementsTests
        {
            private const string Application = "contosoapp";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyEntitlement.AddApplication(null));
                exception.ParamName.Should().Be("application");
            }

            [Fact]
            public void GivenApplicationId_AddsToConfiguration()
            {
                var entitlement = _emptyEntitlement.AddApplication(Application);
                entitlement.Applications.Should().HaveCount(1);
                entitlement.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenDuplicateApplicationId_DoesNotAddToConfiguration()
            {
                var entitlement = _emptyEntitlement
                    .AddApplication(Application)
                    .AddApplication(Application);
                entitlement.Applications.Should().HaveCount(1);
                entitlement.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenApplicationIdWithWhitespace_RemovesWhitespace()
            {
                var entitlement = _emptyEntitlement.AddApplication("  " + Application + "  ");
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
                        () => _emptyEntitlement.AddIpAddress(null));
                exception.ParamName.Should().Be("address");
            }

            [Fact]
            public void GivenIpAddress_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.AddIpAddress(_addressA);
                entitlement.IpAddresses.Should().Contain(_addressA);
            }

            [Fact]
            public void GivenSecondIpAddress_ModifiesConfiguration()
            {
                var entitlement = _emptyEntitlement.AddIpAddress(_addressB);
                entitlement.IpAddresses.Should().Contain(_addressB);
            }

            [Fact]
            public void GivenSecondIpAddress_RetainsFirst()
            {
                var entitlement = _emptyEntitlement
                    .AddIpAddress(_addressA)
                    .AddIpAddress(_addressB);
                entitlement.IpAddresses.Should().Contain(_addressA);
            }
        }

        public class WithIdentifierMethod : NodeEntitlementsTests
        {
            // An identifier to use
            private readonly string _identifier = "an-identifier-for-an-entitlement";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyEntitlement.WithIdentifier(null));
                exception.ParamName.Should().Be("identifier");
            }

            [Fact]
            public void GivenBlank_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyEntitlement.WithIdentifier(string.Empty));
                exception.ParamName.Should().Be("identifier");
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
    }
}
