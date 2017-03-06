using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class MonitoringLoggerTests
    {
        // Discarding fake logger for testing
        private readonly ISimpleLogger _nulLogger;

        // Monitoring Logger for testing
        private readonly MonitoringLogger _monitoringLogger;

        public MonitoringLoggerTests()
        {
            _nulLogger = new NullLogger();
            _monitoringLogger = new MonitoringLogger(_nulLogger);
        }

        public class HaveLoggedErrors : MonitoringLoggerTests
        {
            [Fact]
            public void WhenNoErrorsLogged_ReturnsFalse()
            {
                _monitoringLogger.HaveLoggedErrors.Should().BeFalse();
            }

            [Fact]
            public void AfterErrorsLogged_ReturnsTrue()
            {
                _monitoringLogger.Error("Message");
                _monitoringLogger.HaveLoggedErrors.Should().BeTrue();
            }
        }

        public class HaveLoggedWarnings : MonitoringLoggerTests
        {
            [Fact]
            public void WhenNoWarningsLogged_ReturnsFalse()
            {
                _monitoringLogger.HaveLoggedWarnings.Should().BeFalse();
            }

            [Fact]
            public void AfterWarningsLogged_ReturnsTrue()
            {
                _monitoringLogger.Warning("Message");
                _monitoringLogger.HaveLoggedWarnings.Should().BeTrue();
            }
        }
    }
}
