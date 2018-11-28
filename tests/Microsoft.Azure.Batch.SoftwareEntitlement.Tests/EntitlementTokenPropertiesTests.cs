using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class EntitlementTokenPropertiesTests
    {
        // An empty software entitlement to use for testing
        private readonly EntitlementTokenProperties _emptyTokenProperties = new EntitlementTokenProperties();

        // A Times span representing NZDT
        private readonly TimeSpan _nzdt = new TimeSpan(+13, 0, 0);

        // An instant to use as the start for testing
        private readonly DateTimeOffset _start;

        // An instant to use as the finish for testing
        private readonly DateTimeOffset _finish;

        public EntitlementTokenPropertiesTests()
        {
            _start = new DateTimeOffset(2016, 2, 29, 16, 14, 12, _nzdt);
            _finish = new DateTimeOffset(2016, 3, 31, 16, 14, 12, _nzdt);
        }

        public class WithVirtualMachineIdMethod : EntitlementTokenPropertiesTests
        {
            [Fact]
            public void GivenNull_SetsIdToNull()
            {
                _emptyTokenProperties.WithVirtualMachineId(null)
                    .VirtualMachineId.Should().Be(null);
            }

            [Fact]
            public void GivenVirtualMachineId_ConfiguresProperty()
            {
                const string vmid = "Sample";
                _emptyTokenProperties.WithVirtualMachineId(vmid)
                    .VirtualMachineId.Should().Be(vmid);
            }
        }

        public class FromInstantMethod : EntitlementTokenPropertiesTests
        {
            [Fact]
            public void GivenStart_ConfiguresProperty()
            {
                _emptyTokenProperties.FromInstant(_start)
                    .NotBefore.Should().Be(_start);
            }
        }

        public class UntilInstantMethod : EntitlementTokenPropertiesTests
        {
            [Fact]
            public void GivenFinish_ConfiguresProperty()
            {
                _emptyTokenProperties.UntilInstant(_finish)
                    .NotAfter.Should().Be(_finish);
            }
        }

        public class WithApplicationsMethod : EntitlementTokenPropertiesTests
        {
            private const string Application = "contosoapp";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyTokenProperties.WithApplications(null));
                exception.ParamName.Should().Be("applications");
            }

            [Fact]
            public void GivenApplicationId_AddsToConfiguration()
            {
                var tokenProperties = _emptyTokenProperties.WithApplications(Application);
                tokenProperties.Applications.Should().HaveCount(1);
                tokenProperties.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenDuplicateApplicationId_DoesNotAddToConfiguration()
            {
                var tokenProperties = _emptyTokenProperties
                    .WithApplications(Application, Application);
                tokenProperties.Applications.Should().HaveCount(1);
                tokenProperties.Applications.Should().Contain(Application);
            }

            [Fact]
            public void GivenApplicationIdWithWhitespace_RemovesWhitespace()
            {
                var tokenProperties = _emptyTokenProperties.WithApplications("  " + Application + "  ");
                tokenProperties.Applications.Should().HaveCount(1);
                tokenProperties.Applications.Should().Contain(Application.Trim());
            }
        }

        public class AddIpAddressMethod : EntitlementTokenPropertiesTests
        {
            // sample IPAddresses to use for testing (sample addresses as per RFC5735)
            private readonly IPAddress _addressA = IPAddress.Parse("203.0.113.42");
            private readonly IPAddress _addressB = IPAddress.Parse("203.0.113.44");

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => _emptyTokenProperties.WithIpAddresses(null));
                exception.ParamName.Should().Be("ipAddresses");
            }

            [Fact]
            public void GivenIpAddress_ModifiesConfiguration()
            {
                var tokenProperties = _emptyTokenProperties.WithIpAddresses(_addressA);
                tokenProperties.IpAddresses.Should().Contain(_addressA);
            }

            [Fact]
            public void GivenSecondIpAddress_ModifiesConfiguration()
            {
                var tokenProperties = _emptyTokenProperties.WithIpAddresses(_addressB);
                tokenProperties.IpAddresses.Should().Contain(_addressB);
            }

            [Fact]
            public void GivenSecondIpAddress_RetainsFirst()
            {
                var tokenProperties = _emptyTokenProperties
                    .WithIpAddresses(_addressA, _addressB);
                tokenProperties.IpAddresses.Should().Contain(_addressA);
            }
        }

        public class WithIdentifierMethod : EntitlementTokenPropertiesTests
        {
            // An identifier to use
            private readonly string _identifier = "an-identifier-for-a-token";

            [Fact]
            public void GivenNull_SetsIdentifierToNull()
            {
                var tokenProperties = _emptyTokenProperties.WithIdentifier(null);
                tokenProperties.Identifier.Should().BeNull();
            }

            [Fact]
            public void GivenBlank_SetsIdentifierToEmpty()
            {
                var tokenProperties = _emptyTokenProperties.WithIdentifier(string.Empty);
                tokenProperties.Identifier.Should().BeEmpty();
            }

            [Fact]
            public void GivenIpAddress_ModifiesConfiguration()
            {
                var tokenProperties = _emptyTokenProperties.WithIdentifier(_identifier);
                tokenProperties.Identifier.Should().Be(_identifier);
            }
        }

        public class WithAudienceMethod : EntitlementTokenPropertiesTests
        {
            // An audience to use
            private readonly string _audience = "http://batch.test.example.com/account";

            [Fact]
            public void GivenNull_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyTokenProperties.WithAudience(null));
                exception.ParamName.Should().Be("audience");
            }

            [Fact]
            public void GivenBlank_ThrowsException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => _emptyTokenProperties.WithAudience(string.Empty));
                exception.ParamName.Should().Be("audience");
            }

            [Fact]
            public void GivenAudience_ModifiesConfiguration()
            {
                var tokenProperties = _emptyTokenProperties.WithAudience(_audience);
                tokenProperties.Audience.Should().Be(_audience);
            }
        }

        public class Create : EntitlementTokenPropertiesTests
        {
            private readonly FakeTokenPropertyProvider _validProvider = FakeTokenPropertyProvider.CreateValid();

            [Fact]
            public void GivenValidReader_ReturnsNoErrors()
            {
                // If this test fails, verify that the command line specified by _commandLine 
                // (above) correctly specifies a valid token; If this constraint is violated, most 
                // all of the tests later in this file might fail with spurious errors.
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Errors.Should()
                    .BeEmpty(because: $"the command line represented by {nameof(_validProvider)} should result in a valid token");
            }

            [Fact]
            public void GivenValidReader_ApplicationIdsAreSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.Applications.Should().BeEquivalentTo(_validProvider.ApplicationIds.Value);
            }

            [Fact]
            public void GivenValidReader_AudienceIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.Audience.Should().Be(_validProvider.Audience.Value);
            }

            [Fact]
            public void GivenValidReader_IdentifierIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.Identifier.Should().Be(_validProvider.TokenId.Value);
            }

            [Fact]
            public void GivenValidReader_IpAddressesAreSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.IpAddresses.Should().BeEquivalentTo(_validProvider.IpAddresses.Value);
            }

            [Fact]
            public void GivenValidReader_IssuedAtIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.IssuedAt.Should().Be(_validProvider.IssuedAt.Value);
            }

            [Fact]
            public void GivenValidReader_IssuerIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.Issuer.Should().Be(_validProvider.Issuer.Value);
            }

            [Fact]
            public void GivenValidReader_NotAfterIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.NotAfter.Should().Be(_validProvider.NotAfter.Value);
            }

            [Fact]
            public void GivenValidReader_NotBeforeIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.NotBefore.Should().Be(_validProvider.NotBefore.Value);
            }

            [Fact]
            public void GivenValidReader_VirtualMachineIdIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_validProvider);
                tokenProperties.Value.VirtualMachineId.Should().Be(_validProvider.VirtualMachineId.Value);
            }
        }
    }
}
