using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class ServerOptionBuilderTests
    {
        // One thumbprint string to use for testing
        private readonly string _thumbprint = "1S TR UL EO FC ER TC LU BI SD ON TT AL KA BO UT CE RT CL UB";

        // Server options for testing
        private readonly ServerCommandLine _commandLine = new ServerCommandLine();

        // Fake logger for testing
        private readonly FakeLogger _logger = new FakeLogger();

        // Checker to test
        private readonly ServerOptionBuilder _builder;

        public ServerOptionBuilderTests()
        {
            _builder = new ServerOptionBuilder(_commandLine);
            GlobalLogger.CreateLogger(LogLevel.Debug);
        }

        [Fact]
        public void Build_WithEmptyServerUrl_DoesNotReturnValue()
        {
            _commandLine.ServerUrl = string.Empty;
            var options = _builder.Build();
            options.HasValue.Should().BeFalse();
        }

        [Fact]
        public void Build_WithEmptyServerUrl_HasErrorForServerUrl()
        {
            _commandLine.ServerUrl = string.Empty;
            var options = _builder.Build();
            options.Errors.Should().Contain(e => e.Contains("server endpoint url"));
        }

        [Fact]
        public void Build_WithValidServerUrl_HasNoErrorForServerUrl()
        {
            _commandLine.ServerUrl = "https://example.com";
            var options = _builder.Build();
            options.Errors.Should().NotContain(e => e.Contains("server endpoint url"));
        }

        [Fact]
        public void Build_WithHttpServerUrl_HasErrorForServerUrl()
        {
            _commandLine.ServerUrl = "http://www.example.com";
            var options = _builder.Build();
            options.Errors.Should().Contain(e => e.Contains("Server endpoint url"));
        }

        [Fact]
        public void Build_WithNoConnectionThumbprint_DoesNotReturnValue()
        {
            _commandLine.ConnectionCertificateThumbprint = string.Empty;
            var options = _builder.Build();
            options.HasValue.Should().BeFalse();
        }

        [Fact]
        public void Build_WithNoConnectionThumbprint_HasErrorForConnection()
        {
            _commandLine.ConnectionCertificateThumbprint = string.Empty;
            var options = _builder.Build();
            options.Errors.Should().Contain(e => e.Contains("connection"));
        }

        [Fact]
        public void Build_WithUnknownConnectionThumbprint_HasErrorForConnection()
        {
            _commandLine.ConnectionCertificateThumbprint = _thumbprint;
            var options = _builder.Build();
            options.Errors.Should().Contain(e => e.Contains("connection"));
        }
    }
}
