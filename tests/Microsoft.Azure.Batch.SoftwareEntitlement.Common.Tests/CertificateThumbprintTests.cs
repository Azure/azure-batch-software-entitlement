using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class CertificateThumbprintTests
    {
        // One thumbprint string to use for testing
        protected readonly string _thumbprintA = "NO TA CE RT IF IC AT EN OT HI NG TO SE EH ER EM OV EA LO NG";

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
        }

        public class Constructor : CertificateThumbprintTests
        {
            [Fact]
            public void GivenNull_ThrowsException()
            {
                Assert.Throws<ArgumentNullException>(
                    () => new CertificateThumbprint(null));
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

        public class HasThumbprintMethod : CertificateThumbprintTests
        {
            [Fact]
            public void GivenNull_ReturnsFalse()
            {
                _testThumbprint.HasThumbprint(null).Should().BeFalse();
            }

            [Fact]
            public void GivenEmptyString_ReturnsFalse()
            {
                _testThumbprint.HasThumbprint(string.Empty).Should().BeFalse();
            }

            [Fact]
            public void GivenSameThumbprint_ReturnsTrue()
            {
                _testThumbprint.HasThumbprint(_thumbprintA).Should().BeTrue();
            }

            [Fact]
            public void GivenDifferentThumbprint_ReturnsFalse()
            {
                _testThumbprint.HasThumbprint(_thumbprintB).Should().BeFalse();
            }
        }
    }
}
