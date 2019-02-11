using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
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

        public class Build : EntitlementTokenPropertiesTests
        {
            private readonly FakeTokenPropertyProvider _defaultProvider = FakeTokenPropertyProvider.CreateDefault();

            [Fact]
            public void GivenValidProvider_ReturnsNoErrors()
            {
                // If this test fails, verify that the token provider specified by _defaultProvider 
                // (above) has no errors in any of its properties; If this constraint is violated, most 
                // all of the tests later in this file might fail with spurious errors.
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk($"the token provider represented by {nameof(_defaultProvider)} should have no errors");
            }

            [Fact]
            public void GivenValidProvider_ApplicationIdsAreSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().Applications.Should().BeEquivalentTo(FakeTokenPropertyProvider.DefaultApplicationIds);
            }

            [Fact]
            public void GivenValidProvider_AudienceIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().Audience.Should().Be(FakeTokenPropertyProvider.DefaultAudience);
            }

            [Fact]
            public void GivenValidProvider_IdentifierIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().Identifier.Should().Be(FakeTokenPropertyProvider.DefaultTokenId);
            }

            [Fact]
            public void GivenValidProvider_IpAddressesAreSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().IpAddresses.Should().BeEquivalentTo(FakeTokenPropertyProvider.DefaultIpAddresses);
            }

            [Fact]
            public void GivenValidProvider_IssuedAtIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().IssuedAt.Should().Be(FakeTokenPropertyProvider.DefaultIssuedAt);
            }

            [Fact]
            public void GivenValidProvider_IssuerIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().Issuer.Should().Be(FakeTokenPropertyProvider.DefaultIssuer);
            }

            [Fact]
            public void GivenValidProvider_NotAfterIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().NotAfter.Should().Be(FakeTokenPropertyProvider.DefaultNotAfter);
            }

            [Fact]
            public void GivenValidProvider_NotBeforeIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().NotBefore.Should().Be(FakeTokenPropertyProvider.DefaultNotBefore);
            }

            [Fact]
            public void GivenValidProvider_VirtualMachineIdIsSet()
            {
                var tokenProperties = EntitlementTokenProperties.Build(_defaultProvider);
                tokenProperties.AssertOk().VirtualMachineId.Should().Be(FakeTokenPropertyProvider.DefaultVirtualMachineId);
            }

            [Theory]
            [MemberData(nameof(GetInvalidTokenPropertyProviders))]
            public void GivenInvalidProvider_TokenPropertiesHasError(ITokenPropertyProvider provider, ErrorSet expectedErrors)
            {
                var tokenProperties = EntitlementTokenProperties.Build(provider);
                tokenProperties.AssertError().Should().BeEquivalentTo(expectedErrors);
            }

            public static TheoryData<ITokenPropertyProvider, ErrorSet> GetInvalidTokenPropertyProviders()
            {
                var data = new TheoryData<ITokenPropertyProvider, ErrorSet>();
                void AddData(FakeTokenPropertyProvider provider)
                {
                    var expectedErrors = new List<string>();

                    provider.ApplicationIds.OnError(e => expectedErrors.AddRange(e));
                    provider.Audience.OnError(e => expectedErrors.AddRange(e));
                    provider.IpAddresses.OnError(e => expectedErrors.AddRange(e));
                    provider.NotAfter.OnError(e => expectedErrors.AddRange(e));
                    provider.IssuedAt.OnError(e => expectedErrors.AddRange(e));
                    provider.Issuer.OnError(e => expectedErrors.AddRange(e));
                    provider.NotBefore.OnError(e => expectedErrors.AddRange(e));
                    provider.TokenId.OnError(e => expectedErrors.AddRange(e));
                    provider.VirtualMachineId.OnError(e => expectedErrors.AddRange(e));

                    data.Add(provider, ErrorSet.Create(expectedErrors));
                }

                Result<IEnumerable<string>, ErrorSet> applicationIdsError = ErrorSet.Create("Error providing applicationIds");
                Result<string, ErrorSet> audienceError = ErrorSet.Create("Error providing audience");
                Result<IEnumerable<IPAddress>, ErrorSet> ipAddressesError = ErrorSet.Create("Error providing ipAddresses");
                Result<DateTimeOffset, ErrorSet> notAfterError = ErrorSet.Create("Error providing notAfter");
                Result<DateTimeOffset, ErrorSet> issuedAtError = ErrorSet.Create("Error providing issuedAt");
                Result<string, ErrorSet> issuerError = ErrorSet.Create("Error providing issuer");
                Result<DateTimeOffset, ErrorSet> notBeforeError = ErrorSet.Create("Error providing notBefore");
                Result<string, ErrorSet> tokenIdError = ErrorSet.Create("Error providing tokenId");
                Result<string, ErrorSet> vmidError = ErrorSet.Create("Error providing vmid");

                AddData(CreateProvider(applicationIds: Specify.As(applicationIdsError)));
                AddData(CreateProvider(audience: Specify.As(audienceError)));
                AddData(CreateProvider(ipAddresses: Specify.As(ipAddressesError)));
                AddData(CreateProvider(notAfter: Specify.As(notAfterError)));
                AddData(CreateProvider(issuedAt: Specify.As(issuedAtError)));
                AddData(CreateProvider(issuer: Specify.As(issuerError)));
                AddData(CreateProvider(notBefore: Specify.As(notBeforeError)));
                AddData(CreateProvider(tokenId: Specify.As(tokenIdError)));
                AddData(CreateProvider(vmid: Specify.As(vmidError)));

                return data;
            }

            private static FakeTokenPropertyProvider CreateProvider(
                Specifiable<Result<IEnumerable<string>, ErrorSet>> applicationIds = default,
                Specifiable<Result<string, ErrorSet>> audience = default,
                Specifiable<Result<IEnumerable<IPAddress>, ErrorSet>> ipAddresses = default,
                Specifiable<Result<DateTimeOffset, ErrorSet>> notAfter = default,
                Specifiable<Result<DateTimeOffset, ErrorSet>> issuedAt = default,
                Specifiable<Result<string, ErrorSet>> issuer = default,
                Specifiable<Result<DateTimeOffset, ErrorSet>> notBefore = default,
                Specifiable<Result<string, ErrorSet>> tokenId = default,
                Specifiable<Result<string, ErrorSet>> vmid = default) =>
                new FakeTokenPropertyProvider
                {
                    ApplicationIds = applicationIds.OrDefault(FakeTokenPropertyProvider.DefaultApplicationIds.AsOk()),
                    Audience = audience.OrDefault(FakeTokenPropertyProvider.DefaultAudience.AsOk()),
                    IpAddresses = ipAddresses.OrDefault(FakeTokenPropertyProvider.DefaultIpAddresses.AsOk()),
                    NotAfter = notAfter.OrDefault(FakeTokenPropertyProvider.DefaultNotAfter.AsOk()),
                    IssuedAt = issuedAt.OrDefault(FakeTokenPropertyProvider.DefaultIssuedAt.AsOk()),
                    Issuer = issuer.OrDefault(FakeTokenPropertyProvider.DefaultIssuer.AsOk()),
                    NotBefore = notBefore.OrDefault(FakeTokenPropertyProvider.DefaultNotBefore.AsOk()),
                    TokenId = tokenId.OrDefault(FakeTokenPropertyProvider.DefaultTokenId.AsOk()),
                    VirtualMachineId = vmid.OrDefault(FakeTokenPropertyProvider.DefaultVirtualMachineId.AsOk())
                };
        }
    }
}
