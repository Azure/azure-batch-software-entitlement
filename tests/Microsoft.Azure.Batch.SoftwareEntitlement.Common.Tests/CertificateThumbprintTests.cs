using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class CertificateThumbprintTests
    {
        // One thumbprint string to use for testing
        protected readonly string _thumbprintA = "NO TA CE RT IF IC AT EN OT HI NG TO SE EH ER EM OV EA LO NG";

        // A variation on _thumbprintA for testing
        protected readonly string _thumbprintAwithoutSpaces;

        // Another thumbprint string to use for testing
        protected readonly string _thumbprintB = "TH IS IS NO TT HE CE RT IF IC AT EY OU AR EL OO KI NG FO RX";

        // A thumbprint for testing
        protected readonly CertificateThumbprint _testThumbprint;

        // Another thumbprint for testing, identical to the one above
        protected readonly CertificateThumbprint _identicalThumbprint;

        // Another thumbprint, this one differs from the two above
        protected readonly CertificateThumbprint _otherThumbprint;

        public CertificateThumbprintTests()
        {
            _testThumbprint = new CertificateThumbprint(_thumbprintA);
            _identicalThumbprint = new CertificateThumbprint(_thumbprintA);
            _otherThumbprint = new CertificateThumbprint(_thumbprintB);

            _thumbprintAwithoutSpaces = string.Join("", _thumbprintA.Where(c => c != ' '));
        }

        public class Constructor : CertificateThumbprintTests
        {
            [Fact]
            public void GivenNull_ThrowsArgumentException()
            {
                var exception =
                    Assert.Throws<ArgumentException>(
                        () => new CertificateThumbprint(null));
                exception.ParamName.Should().Be("thumbprint");
            }
        }

        public class EqualsMethod : CertificateThumbprintTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _testThumbprint.Equals(null).Should().BeFalse();
            }

            [Fact]
            public void GivenSelf_ReturnsTrue()
            {
                _testThumbprint.Equals(_testThumbprint).Should().BeTrue();
            }

            [Fact]
            public void GivenIdenticalThumbprint_ReturnsTrue()
            {
                _testThumbprint.Equals(_identicalThumbprint).Should().BeTrue();
            }

            [Fact]
            public void GivenDifferentThumbprint_ReturnsFalse()
            {
                _testThumbprint.Equals(_otherThumbprint).Should().BeFalse();
            }
        }

        public class MatchesMethod : CertificateThumbprintTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _testThumbprint.Matches(null).Should().BeFalse();
            }

            [Fact]
            public void GivenEmptyString_ReturnsFalse()
            {
                _testThumbprint.Matches(string.Empty).Should().BeFalse();
            }

            [Fact]
            public void GivenSameThumbprint_ReturnsTrue()
            {
                _testThumbprint.Matches(_thumbprintA).Should().BeTrue();
            }

            [Fact]
            public void GivenLowercasedThumbprint_ReturnsTrue()
            {
                var thumbprint = _thumbprintA.ToLowerInvariant();
                _testThumbprint.Matches(thumbprint).Should().BeTrue();
            }

            [Fact]
            public void GivenUppercasedThumbprint_ReturnsTrue()
            {
                var thumbprint = _thumbprintA.ToUpperInvariant();
                _testThumbprint.Matches(thumbprint).Should().BeTrue();
            }

            [Fact]
            public void GivenThumbprintWithSpacesRemoved_ReturnsTrue()
            {
                _testThumbprint.Matches(_thumbprintAwithoutSpaces).Should().BeTrue();
            }

            [Fact]
            public void GivenDifferentThumbprint_ReturnsFalse()
            {
                _testThumbprint.Matches(_thumbprintB).Should().BeFalse();
            }
        }

        public class GetHashCodeMethod : CertificateThumbprintTests
        {
            [Fact]
            public void ForIdenticalInstances_ReturnsSameResult()
            {
                _testThumbprint.GetHashCode().Should().Be(_identicalThumbprint.GetHashCode());
            }

            [Fact]
            public void ForEquivalentThumbprints_ReturnsSameResult()
            {
                var other = new CertificateThumbprint(_thumbprintAwithoutSpaces);
                other.GetHashCode().Should().Be(_testThumbprint.GetHashCode());
            }
        }
    }
}
