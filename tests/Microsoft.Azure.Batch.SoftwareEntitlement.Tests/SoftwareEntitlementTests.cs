using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class SoftwareEntitlementTests
    {
        // An empty software entitlement to use for testing
        private readonly SoftwareEntitlement _emptyEntitlement = new SoftwareEntitlement(NullLogger.Instance);

        // A Times span representing NZDT
        private readonly TimeSpan _nzdt = new TimeSpan(+13, 0, 0);

        // An instant to use as the start for testing
        private DateTimeOffset _start;

        // An instant to use as the finish for testing
        private DateTimeOffset _finish;

        public SoftwareEntitlementTests()
        {
            _start = new DateTimeOffset(2016, 2, 29, 16, 14, 12, _nzdt);
            _finish = new DateTimeOffset(2016, 3, 31, 16, 14, 12, _nzdt);
        }

        public class WithVirtualMachineIdMethod : SoftwareEntitlementTests
        {
            [Fact]
            public void GivenNull_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(
                    () => _emptyEntitlement.WithVirtualMachineId(null));
            }

            [Fact]
            public void GivenVirtualMachineId_ConfiguresProperty()
            {
                var vmid = "Sample";
                _emptyEntitlement.WithVirtualMachineId(vmid)
                    .VirtualMachineId.Should()
                    .Be(vmid);
            }
        }

        public class ForTimeRangeMethod : SoftwareEntitlementTests
        {
            [Fact]
            public void GivenStart_ConfiguresProperty()
            {
                _emptyEntitlement.ForTimeRange(_start, _finish)
                    .NotBefore
                    .Should()
                    .Be(_start);
            }

            [Fact]
            public void GivenFinish_ConfiguresProperty()
            {
                _emptyEntitlement.ForTimeRange(_start, _finish)
                    .NotAfter
                    .Should()
                    .Be(_finish);
            }
        }
    }
}
