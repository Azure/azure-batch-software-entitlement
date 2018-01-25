using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public class ServerOptionBuilderFake : ServerOptionBuilder
    {
        private readonly ServerOptionBuilderOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOptionBuilder"/> class
        /// </summary>
        /// <param name="commandLine">Options provided on the command line.</param>
        /// <param name="options">Options for configuring validation.</param>
        public ServerOptionBuilderFake(
            ServerCommandLine commandLine,
            ServerOptionBuilderOptions options)
            : base(commandLine)
        {
            _options = options;
        }

        protected override Errorable<Uri> ServerUrl()
        {
            if (_options.HasFlag(ServerOptionBuilderOptions.ServerUrlOptional))
            {
                return Errorable.Success(new Uri("http://test"));
            }

            return base.ServerUrl();
        }

        protected override Errorable<X509Certificate2> ConnectionCertificate()
        {
            if (_options.HasFlag(ServerOptionBuilderOptions.ConnectionThumbprintOptional))
            {
                return Errorable.Success<X509Certificate2>(null);
            }

            return base.ConnectionCertificate();
        }
    }

    [Flags]
    public enum ServerOptionBuilderOptions
    {
        None,
        ServerUrlOptional,
        ConnectionThumbprintOptional
    }
}