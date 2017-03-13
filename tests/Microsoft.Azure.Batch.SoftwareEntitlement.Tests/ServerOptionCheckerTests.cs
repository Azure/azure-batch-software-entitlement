using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class ServerOptionCheckerTests
    {
        // Server options for testing
        private readonly ServerOptions _options = new ServerOptions();

        // Fake logger for testing
        private readonly FakeLogger _logger = new FakeLogger();

        // Checker to test
        private readonly ServerOptionChecker _checker;

        public ServerOptionCheckerTests()
        {
            _checker = new ServerOptionChecker(_options, _logger);
        }

        public class ServerUrlProperty : ServerOptionCheckerTests
        {
            [Fact]
            public void WhenOptionsHasEmptyServerUri_ReturnsNull()
            {
                _options.ServerUrl = string.Empty;
                _checker.ServerUrl.Should().BeNull();
            }

            [Fact]
            public void WhenOptionsHasEmptyServerUri_LogsError()
            {
                _options.ServerUrl = string.Empty;
                var serverUrl = _checker.ServerUrl;
                _logger.HasErrors.Should().BeTrue();
            }

            [Fact]
            public void WhenOptionsHasValidUri_ReturnsInstance()
            {
                _options.ServerUrl = "https://www.example.com";
                _checker.ServerUrl.Should().NotBeNull();
            }

            [Fact]
            public void WhenOptionsHasAnHttpUri_ReturnsNull()
            {
                _options.ServerUrl = "http://www.example.com";
                _checker.ServerUrl.Should().BeNull();
            }

            [Fact]
            public void WhenOptionsHasAnHttpUri_LogsError()
            {
                _options.ServerUrl = "http://www.example.com";
                var serverUrl = _checker.ServerUrl;
                _logger.HasErrors.Should().BeTrue();
            }
        }

        public class ConnectionThumbprintProperty : ServerOptionCheckerTests
        {
            // One thumbprint string to use for testing
            private readonly string _thumbprint= "1S TR UL EO FC ER TC LU BI SD ON TT AL KA BO UT CE RT CL UB";

            [Fact]
            public void WhenOptionsHasNoThumbprint_ReturnsNull()
            {
                _options.ConnectionCertificateThumbprint = string.Empty;
                _checker.ConnectionThumbprint.Should().BeNull();
            }

            [Fact]
            public void WhenOptionsHasNoThumbprint_LogsError()
            {
                _options.ConnectionCertificateThumbprint = string.Empty;
                var thumbprint = _checker.ConnectionThumbprint;
                _logger.HasErrors.Should().BeTrue();
            }

            [Fact]
            public void WhenOptionsHasNoThumbprint_ReturnsInstance()
            {
                _options.ConnectionCertificateThumbprint = _thumbprint;
                _checker.ConnectionThumbprint.Should().NotBeNull();
            }

            [Fact]
            public void WhenOptionsHasNoThumbprint_ReturnsInstanceWithExpectedThumbprint()
            {
                _options.ConnectionCertificateThumbprint = _thumbprint;
                _checker.ConnectionThumbprint.HasThumbprint(_thumbprint).Should().BeTrue();
            }
        }

    }
}
