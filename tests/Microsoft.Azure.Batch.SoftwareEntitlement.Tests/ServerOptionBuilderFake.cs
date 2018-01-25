using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    /// <summary>
    /// A fake version of <see cref="ServerOptionBuilder"/> that allows some validation to be disabled 
    /// to allow testing of other code paths
    /// </summary>
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

        /// <summary>
        /// Find the server URL for our hosting
        /// </summary>
        /// <remarks>
        /// Can be disabled by passing <see cref="ServerOptionBuilderOptions.ServerUrlOptional"/> 
        /// to the constructor.
        /// </remarks>
        /// <returns>An <see cref="Errorable{Uri}"/> containing either the URL to use or any 
        /// relevant errors.</returns>
        protected override Errorable<Uri> ServerUrl()
        {
            if (_options.HasFlag(ServerOptionBuilderOptions.ServerUrlOptional))
            {
                return Errorable.Success(new Uri("http://test"));
            }

            return base.ServerUrl();
        }

        /// <summary>
        /// Find the certificate to use for HTTPS connections
        /// </summary>
        /// <remarks>
        /// Can be disabled by passing <see cref="ServerOptionBuilderOptions.ConnectionThumbprintOptional"/> 
        /// to the constructor.
        /// </remarks>
        /// <returns>Certificate, if found; error details otherwise.</returns>
        protected override Errorable<X509Certificate2> ConnectionCertificate()
        {
            if (_options.HasFlag(ServerOptionBuilderOptions.ConnectionThumbprintOptional))
            {
                return Errorable.Success<X509Certificate2>(null);
            }

            return base.ConnectionCertificate();
        }
    }

    /// <summary>
    /// Options used to disable selected validation with <see cref="ServerOptionBuilder"/> 
    /// for testing purposes.
    /// </summary>
    [Flags]
    public enum ServerOptionBuilderOptions
    {
        None,
        ServerUrlOptional,
        ConnectionThumbprintOptional
    }
}