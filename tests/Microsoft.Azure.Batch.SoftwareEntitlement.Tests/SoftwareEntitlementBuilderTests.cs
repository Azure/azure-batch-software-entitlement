using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class SoftwareEntitlementBuilderTests
    {
        // A valid set of command line arguments for testing
        private readonly GenerateCommandLine _commandLine = new GenerateCommandLine()
        {
            VirtualMachineId = "Sample"
        };

        public class BuildMethod : SoftwareEntitlementBuilderTests
        {

            [Fact]
            public void GivenNullCommandLine_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(
                    () => SoftwareEntitlementBuilder.Build(null));
            }
        }

        public class VirtualMachineIdProperty : SoftwareEntitlementBuilderTests
        {
            [Fact]
            public void WhenMissing_BuildDoesNotReturnValue()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.HasValue.Should().BeFalse();
            }

            [Fact]
            public void WhenMissing_BuildReturnsErrorForVirtualMachineId()
            {
                _commandLine.VirtualMachineId = null;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("virtual machine identifier"));
            }

            [Fact]
            public void WithId_BuildReturnsNoErrorForVirtualMachineId()
            {
                _commandLine.VirtualMachineId = "virtualMachine";
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("virtual machine identifier"));
            }

            [Fact]
            public void WithId_PropertyIsSet()
            {
                var virtualMachineId = "virtualMachine";
                _commandLine.VirtualMachineId = virtualMachineId;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Value.VirtualMachineId.Should().Be(virtualMachineId);
            }
        }

        public class NotBeforeProperty : SoftwareEntitlementBuilderTests
        {
            private readonly string _validNotBefore =
                DateTimeOffset.Now.ToString(TimestampParser.ExpectedFormat);

            [Fact]
            public void WhenMissing_BuildReturnsValue()
            {
                _commandLine.NotBefore = "";
                var result = SoftwareEntitlementBuilder.Build(_commandLine);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenInvalid_BuildReturnsErrorForNotBefore()
            {
                _commandLine.NotBefore = "Not a timestamp";
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("NotBefore"));
            }

            [Fact]
            public void WhenValid_BuildReturnsNoErrorForNotBefore()
            {
                _commandLine.NotBefore = _validNotBefore;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("NotBefore"));
            }

            [Fact]
            public void WhenValid_PropertyIsSet()
            {
                _commandLine.NotBefore = _validNotBefore;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotBefore.ToString(TimestampParser.ExpectedFormat)
                    .Should().Be(_validNotBefore);
            }
        }

        public class NotAfterProperty : SoftwareEntitlementBuilderTests
        {
            private readonly string _validNotAfter =
                DateTimeOffset.Now.AddDays(7).ToString(TimestampParser.ExpectedFormat);

            [Fact]
            public void WhenMissing_BuildReturnsValue()
            {
                _commandLine.NotAfter = "";
                var result = SoftwareEntitlementBuilder.Build(_commandLine);
                result.HasValue.Should().BeTrue();
            }

            [Fact]
            public void WhenInvalid_BuildReturnsErrorForNotAfter()
            {
                _commandLine.NotAfter = "Not a timestamp";
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().Contain(e => e.Contains("NotAfter"));
            }

            [Fact]
            public void WhenValid_BuildReturnsNoErrorForNotAfter()
            {
                _commandLine.NotAfter = _validNotAfter;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                entitlement.Errors.Should().NotContain(e => e.Contains("NotAfter"));
            }

            [Fact]
            public void WhenValid_PropertyIsSet()
            {
                _commandLine.NotAfter = _validNotAfter;
                var entitlement = SoftwareEntitlementBuilder.Build(_commandLine);
                // Compare as formatted strings to avoid issues with extra seconds we don't care about
                entitlement.Value.NotAfter.ToString(TimestampParser.ExpectedFormat)
                    .Should().Be(_validNotAfter);
            }
        }
    }
}
