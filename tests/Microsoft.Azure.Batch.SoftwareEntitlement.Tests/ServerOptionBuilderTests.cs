using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    /// <summary>
    /// Negative tests to ensure the <see cref="ServerOptionBuilder"/> correctly reports error 
    /// cases for each possible parameter
    /// </summary>
    public class ServerOptionBuilderTests
    {
        // One thumbprint string to use for testing
        private readonly string _thumbprint = "1S TR UL EO FC ER TC LU BI SD ON TT AL KA BO UT CE RT CL UB";

        // Server options for testing
        private readonly ServerCommandLine _commandLine = new ServerCommandLine();

        [Fact]
        public void Build_WithEmptyServerUrl_DoesNotReturnValue()
        {
            var options = ServerOptionBuilder.Build(_commandLine);
            options.HasValue.Should().BeFalse();
        }

        [Fact]
        public void Build_WithEmptyServerUrl_HasErrorForServerUrl()
        {
            _commandLine.ServerUrl = string.Empty;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Errors.Should().Contain(e => e.Contains("server endpoint URL"));
        }

        [Fact]
        public void Build_WithValidServerUrl_HasNoErrorForServerUrl()
        {
            _commandLine.ServerUrl = "https://example.com";
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Errors.Should().NotContain(e => e.Contains("server endpoint URL"));
        }

        [Fact]
        public void Build_WithHttpServerUrl_HasErrorForServerUrl()
        {
            _commandLine.ServerUrl = "http://www.example.com";
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Errors.Should().Contain(e => e.Contains("Server endpoint URL"));
        }

        [Fact]
        public void Build_WithNoConnectionThumbprint_DoesNotReturnValue()
        {
            _commandLine.ConnectionCertificateThumbprint = string.Empty;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.HasValue.Should().BeFalse();
        }

        [Fact]
        public void Build_WithNoConnectionThumbprint_HasErrorForConnection()
        {
            _commandLine.ConnectionCertificateThumbprint = string.Empty;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Errors.Should().Contain(e => e.Contains("connection"));
        }

        [Fact]
        public void Build_WithUnknownConnectionThumbprint_HasErrorForConnection()
        {
            _commandLine.ConnectionCertificateThumbprint = _thumbprint;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Errors.Should().Contain(e => e.Contains("connection"));
        }

        [Fact]
        public void Build_WithEmptyAudience_HasDefaultValueForAudience()
        {
            _commandLine.Audience = string.Empty;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Value.Audience.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Build_WithEmptyIssuer_HasDefaultValueForIssuer()
        {
            _commandLine.Issuer = string.Empty;
            var options = ServerOptionBuilder.Build(_commandLine);
            options.Value.Issuer.Should().NotBeNullOrEmpty();
        }
    }
}
