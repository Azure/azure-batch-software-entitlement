using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    /// <summary>
    /// Tests to ensure the <see cref="ServerOptionBuilder"/> correctly reports error 
    /// cases for each possible parameter
    /// </summary>
    public class ServerOptionBuilderTests
    {
        // One thumbprint string to use for testing
        private readonly string _thumbprint = "1S TR UL EO FC ER TC LU BI SD ON TT AL KA BO UT CE RT CL UB";

        // Server options for testing
        private readonly ServerCommandLine _commandLine = new ServerCommandLine();

        // Permissive options to bypass mandatory errors when testing other properties
        private readonly ServerOptionBuilderOptions _permissiveOptions =
            ServerOptionBuilderOptions.ServerUrlOptional
            | ServerOptionBuilderOptions.ConnectionThumbprintOptional;

        public class ServerUrl : ServerOptionBuilderTests
        {
            [Fact]
            public void Build_WithEmptyServerUrl_DoesNotReturnValue()
            {
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.HasValue.Should().BeFalse();
            }

            [Fact]
            public void Build_WithEmptyServerUrl_HasErrorForServerUrl()
            {
                _commandLine.ServerUrl = string.Empty;
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.Errors.Should().Contain(e => e.Contains("server endpoint URL"));
            }

            [Fact]
            public void Build_WithHttpServerUrl_HasErrorForServerUrl()
            {
                _commandLine.ServerUrl = "http://www.example.com";
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.Errors.Should().Contain(e => e.Contains("Server endpoint URL"));
            }

            [Fact]
            public void WithValidServerUrl_ConfigureServerUrl()
            {
                _commandLine.ServerUrl = "https://example.com/";
                var options = new ServerOptionBuilderFake(
                    _commandLine, ServerOptionBuilderOptions.ConnectionThumbprintOptional)
                    .Build();
                options.Value.ServerUrl.ToString().Should().Be(_commandLine.ServerUrl);
            }
        }

        public class ConnectionThumbprint : ServerOptionBuilderTests
        {
            [Fact]
            public void Build_WithNoConnectionThumbprint_DoesNotReturnValue()
            {
                _commandLine.ConnectionCertificateThumbprint = string.Empty;
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.HasValue.Should().BeFalse();
            }

            [Fact]
            public void Build_WithNoConnectionThumbprint_HasErrorForConnection()
            {
                _commandLine.ConnectionCertificateThumbprint = string.Empty;
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.Errors.Should().Contain(e => e.Contains("connection"));
            }

            [Fact]
            public void Build_WithUnknownConnectionThumbprint_HasErrorForConnection()
            {
                _commandLine.ConnectionCertificateThumbprint = _thumbprint;
                var options = new ServerOptionBuilder(_commandLine).Build();
                options.Errors.Should().Contain(e => e.Contains("connection"));
            }
        }

        public class Audience : ServerOptionBuilderTests
        {
            [Fact]
            public void Build_WithEmptyAudience_HasDefaultValueForAudience()
            {
                _commandLine.Audience = string.Empty;
                var options = new ServerOptionBuilderFake(_commandLine, _permissiveOptions).Build();
                options.Value.Audience.Should().NotBeNullOrEmpty();
            }
        }

        public class Issuer : ServerOptionBuilderTests
        {
            [Fact]
            public void Build_WithEmptyIssuer_HasDefaultValueForIssuer()
            {
                _commandLine.Issuer = string.Empty;
                var options = new ServerOptionBuilderFake(_commandLine, _permissiveOptions).Build();
                options.Value.Issuer.Should().NotBeNullOrEmpty();
            }
        }

        public class ExitAfterRequest : ServerOptionBuilderTests
        {
            [Fact]
            public void Build_WithoutExitAfterRequest_ShouldHaveDefault()
            {
                _commandLine.ExitAfterRequest = false;
                var options = new ServerOptionBuilderFake(_commandLine, _permissiveOptions).Build();
                options.Value.ExitAfterRequest.Should().BeFalse();
            }

            [Fact]
            public void Build_WithExitAfterRequest_ConfiguresValue()
            {
                _commandLine.ExitAfterRequest = true;
                var options = new ServerOptionBuilderFake(_commandLine, _permissiveOptions).Build();
                options.Value.ExitAfterRequest.Should().BeTrue();
            }
        }
    }
}
