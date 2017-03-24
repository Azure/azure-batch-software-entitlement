using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class StringExtensionsTests
    {
        public class AsLinesMethod : StringExtensionsTests
        {
            [Theory]
            [InlineData("A simple string", 1)]
            [InlineData("A simple string\nWith another line", 2)]
            [InlineData("A simple string\r\nWith another line", 2)]
            [InlineData("Alpha\nBeta\nGamma\nDelta\nEpsilon\nSigma\nPhi", 7)]
            [InlineData("A simple string with EOLN at the end is one line\n", 1)]
            public void GivenString_ReturnsExpectedCount(string content, int count)
            {
                content.AsLines().Should().HaveCount(count);
            }
        }
    }
}
