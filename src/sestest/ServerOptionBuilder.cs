using System;
using System.Collections.Generic;
using System.Linq;
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
        // Reference to the server command line we wrap
        private readonly ServerCommandLine _commandLine;

        // Reference to a store in which we can search for certificates
        private readonly CertificateStore _certificateStore = new CertificateStore();

        /// <summary>
        /// Build an instance of <see cref="ServerOptions"/> from the information supplied on the 
        /// command line by the user
        /// </summary>
        /// <param name="commandLine">Command line parameters supplied by the user.</param>
        /// <returns>Either a usable (and completely valid) <see cref="ServerOptions"/> or a set 
        /// of errors.</returns>
        public static Errorable<ServerOptions> Build(ServerCommandLine commandLine)
        {
            var builder = new ServerOptionBuilder(commandLine);
            return builder.Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOptionBuilder"/> class
        /// </summary>
        /// <param name="commandLine">Options provided on the command line.</param>
        private ServerOptionBuilder(ServerCommandLine commandLine)
        {
            _commandLine = commandLine;
        }

        /// <summary>
        /// Build an instance of <see cref="ServerOptions"/> from the information supplied on the 
        /// command line by the user
        /// </summary>
        /// <returns>Either a usable (and completely valid) <see cref="ServerOptions"/> or a set 
        /// of errors.</returns>
        private Errorable<ServerOptions> Build()
        {
            var options = new ServerOptions();
            var errors = new List<string>();

            void Configure<V>(Func<Errorable<V>> readConfiguration, Func<V, ServerOptions> applyConfiguration)
            {
                readConfiguration().Match(
                    whenSuccessful: value => options = applyConfiguration(value),
                    whenFailure: e => errors.AddRange(e));
            }

            Configure(ServerUrl, url => options.WithServerUrl(url));
            Configure(ConnectionCertificate, cert => options.WithConnectionCertificate(cert));
            Configure(SigningCertificate, cert => options.WithSigningCertificate(cert));
            Configure(EncryptingCertificate, cert => options.WithConnectionCertificate(cert));

            if (errors.Any())
            {
                return Errorable.Failure<ServerOptions>(errors);
            }

            return Errorable.Success(options);
        }

        /// <summary>
        /// Find the server URL for our hosting
        /// </summary>
        /// <returns>An <see cref="Errorable{Uri}"/> containing either the URL to use or any 
        /// relevant errors.</returns>
        private Errorable<Uri> ServerUrl()
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
        /// <returns>Certificate, if found; error details otherwise.</returns>
        private Errorable<X509Certificate2> ConnectionCertificate()
        {
            return FindCertificate("connection", _commandLine.ConnectionCertificateThumbprint);
        }

        /// <summary>
        /// Find the certificate to use for signing tokens
        /// </summary>
        /// <returns>Certificate, if found; error details otherwise.</returns>
        private Errorable<X509Certificate2> SigningCertificate()
        {
            return FindCertificate("signing", _commandLine.SigningCertificateThumbprint);
        }

        /// <summary>
        /// Find the certificate to use for encrypting tokens
        /// </summary>
        /// <returns>Certificate, if found; error details otherwise.</returns>
        private Errorable<X509Certificate2> EncryptingCertificate()
        {
            return FindCertificate("encrypting", _commandLine.EncryptionCertificateThumbprint);
        }

        /// <summary>
        /// Find a certificate for a specified purpose, given a thumbprint
        /// </summary>
        /// <param name="purpose">A use for which the certificate is needed (for human consumption).</param>
        /// <param name="thumbprint">Thumbprint of the required certificate.</param>
        /// <returns>The certificate, if found; an error message otherwise.</returns>
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
