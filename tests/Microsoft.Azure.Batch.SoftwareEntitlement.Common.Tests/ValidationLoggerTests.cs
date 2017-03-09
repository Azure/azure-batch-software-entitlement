using Microsoft.Extensions.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public class ValidationLoggerTests
    {
        // Monitoring Logger for testing
        private readonly ValidationLogger _validationLogger;

        public ValidationLoggerTests()
        {
            _validationLogger = new ValidationLogger(NullLogger.Instance);
        }

        public class HaveLoggedErrors : ValidationLoggerTests
        {
            [Fact]
            public void WhenNoErrorsLogged_ReturnsFalse()
            {
                _validationLogger.HasErrors.Should().BeFalse();
            }

            [Fact]
            public void AfterErrorsLogged_ReturnsTrue()
            {
                _validationLogger.LogError("Message");
                _validationLogger.HasErrors.Should().BeTrue();
            }
        }

        public class HaveLoggedWarnings : ValidationLoggerTests
        {
            [Fact]
            public void WhenNoWarningsLogged_ReturnsFalse()
            {
                _validationLogger.HasWarnings.Should().BeFalse();
            }

            [Fact]
            public void AfterWarningsLogged_ReturnsTrue()
            {
                _validationLogger.LogWarning("Message");
                _validationLogger.HasWarnings.Should().BeTrue();
            }
        }
    }
}
