using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class CertificatesTests
    {
        public class SanitizeThumbprintMethod
        {
            [Fact]
            public void GivenEmptyString_ReturnsEmptyString()
            {
                Certificates.SanitizeThumbprint(string.Empty).Should().Be(String.Empty);
            }

            [Fact]
            public void GivenSimpleThumbprint_ReturnsSameString()
            {
                var thumbprint = "e000bfae3248b31f697832208250d4fd1ef97e2f";
                Certificates.SanitizeThumbprint(thumbprint).Should().Be(thumbprint);
            }

            [Fact]
            public void GivenCopiedThumbprint_ReturnsSafeThumbprint()
            {
                var thumbprint = "e0 00 bf ae 32 48 b3 1f 69 78 32 20 82 50 d4 fd 1e f9 7e 2f";
                var safeThumbprint = "e000bfae3248b31f697832208250d4fd1ef97e2f";
                Certificates.SanitizeThumbprint(thumbprint).Should().Be(safeThumbprint);
            }
        }
    }
}
