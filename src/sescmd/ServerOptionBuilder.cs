using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// A factory object that tries to create a <see cref="ServerOptions"/> instance when given the 
    /// <see cref="ServerCommandLine"/> specified by the user.
    /// </summary>
    public class ServerOptionBuilder
    {
        // Reference to the server options we wrap
        private readonly ServerCommandLine _commandLine;

        // Reference to a store in which we can search for certificates
        private readonly CertificateStore _certificateStore = new CertificateStore();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOptionBuilder"/> class
        /// </summary>
        /// <param name="commandLine">Options provided on the command line.</param>
        public ServerOptionBuilder(ServerCommandLine commandLine)
        {
            _commandLine = commandLine;
        }

        public Errorable<ServerOptions> Build()
        {
            var serverUrl = FindServerUrl();
            var connectionCertificate = FindConnectionCertificate();

            var result = Errorable<ServerOptions>.Success(new ServerOptions())
                .Apply(connectionCertificate, (options, certificate) => options.WithConnectionCertificate(certificate))
                .Apply(serverUrl, (options, url) => options.WithServerUrl(url));

            return result;
        }

        /// <summary>
        /// Find the server URL for our hosting
        /// </summary>
        /// <returns>An <see cref="Errorable{Uri}"/> containing either the URL to use or any 
        /// relevant errors.</returns>
        private Errorable<Uri> FindServerUrl()
        {
            if (string.IsNullOrWhiteSpace(_commandLine.ServerUrl))
            {
                return Errorable.Failure<Uri>("No server endpoint URL specified.");
            }

            try
            {
                var result = new Uri(_commandLine.ServerUrl);
                if (!result.HasScheme("https"))
                {
                    return Errorable.Failure<Uri>("Server endpoint URL must specify https://");
                }

                return Errorable.Success(result);
            }
            catch (Exception e)
            {
                return Errorable.Failure<Uri>($"Invalid server endpoint URL specified ({e.Message})");
            }
        }

        /// <summary>
        /// Find the certificate to use for HTTPS connections
        /// </summary>
        /// <returns>Certificate, if found; null otherwise.</returns>
        private Errorable<X509Certificate2> FindConnectionCertificate()
        {
            return FindCertificate("connection", _commandLine.ConnectionCertificateThumbprint);
        }

        /// <summary>
        /// Find a certificate for a specified purpose, given a thumbprint
        /// </summary>
        /// <param name="purpose">A use for which the certificate is needed (for human consumption).</param>
        /// <param name="thumbprint">Thumbprint of the required certificate.</param>
        /// <returns></returns>
        private Errorable<X509Certificate2> FindCertificate(string purpose, string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return Errorable.Failure<X509Certificate2>($"No thumbprint supplied; unable to find a {purpose} certificate.");
            }

            var certificateThumbprint = new CertificateThumbprint(thumbprint);
            return _certificateStore.FindByThumbprint(purpose, certificateThumbprint);
        }
    }
}
