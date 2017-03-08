using Microsoft.Extensions.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class MonitoringLoggerTests
    {
        // Monitoring Logger for testing
        private readonly MonitoringLogger _monitoringLogger;

        public MonitoringLoggerTests()
        {
            _monitoringLogger = new MonitoringLogger(NullLogger.Instance);
        }

        public class HaveLoggedErrors : MonitoringLoggerTests
        {
            [Fact]
            public void WhenNoErrorsLogged_ReturnsFalse()
            {
                _monitoringLogger.HasErrors.Should().BeFalse();
            }

            [Fact]
            public void AfterErrorsLogged_ReturnsTrue()
            {
                _monitoringLogger.LogError("Message");
                _monitoringLogger.HasErrors.Should().BeTrue();
            }
        }

        public class HaveLoggedWarnings : MonitoringLoggerTests
        {
            [Fact]
            public void WhenNoWarningsLogged_ReturnsFalse()
            {
                _monitoringLogger.HasWarnings.Should().BeFalse();
            }

            [Fact]
            public void AfterWarningsLogged_ReturnsTrue()
            {
                _monitoringLogger.LogWarning("Message");
                _monitoringLogger.HasWarnings.Should().BeTrue();
            }
        }
    }
}
