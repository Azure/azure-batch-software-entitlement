using System;
using System.Collections.Generic;
using System.Text;
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
            VirtualMachineId = "Sample"
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
            public void WhenMissing_BuildDoesNotReturnValue()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenMissing_BuildReturnsErrorForVirtualMachineId()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("virtual machine identifier"));
            }

            [Fact]
            public void WithId_BuildReturnsNoErrorForVirtualMachineId()
            {
                _commandLine.VirtualMachineId = "virtualMachine";
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("virtual machine identifier"));
            }

            [Fact]
            public void WithId_PropertyIsSet()
            {
                var virtualMachineId = "virtualMachine";
                _commandLine.VirtualMachineId = virtualMachineId;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                entitlement.Value.VirtualMachineId.Should().Be(virtualMachineId);
            }
        }

        public class NotBeforeProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _validNotBefore =
                DateTimeOffset.Now.ToString(TimestampParser.ExpectedFormat);

            [Fact]
            public void WhenMissing_BuildReturnsValue()
            {
                _commandLine.NotBefore = "";
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
            public void WhenValid_PropertyIsSet()
            {
                _commandLine.NotBefore = _validNotBefore;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotBefore.ToString(TimestampParser.ExpectedFormat)
                    .Should().Be(_validNotBefore);
            }
        }

        public class NotAfterProperty : NodeEntitlementsBuilderTests
        {
            private readonly string _validNotAfter =
                DateTimeOffset.Now.AddDays(7).ToString(TimestampParser.ExpectedFormat);

            [Fact]
            public void WhenMissing_BuildReturnsValue()
            {
                _commandLine.NotAfter = "";
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
            public void WhenValid_PropertyIsSet()
            {
                _commandLine.NotAfter = _validNotAfter;
                var entitlement = NodeEntitlementsBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotAfter.ToString(TimestampParser.ExpectedFormat)
                    .Should().Be(_validNotAfter);
            }
        }
    }
}
