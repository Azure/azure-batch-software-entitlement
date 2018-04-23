using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Options for running as a standalone server
    /// </summary>
    /// <remarks>Built by a <see cref="ServerOptionBuilder"/> from an instance of 
    /// <see cref="ServerCommandLine"/> and only used if all validation passes.</remarks>
    public class ServerOptions
    {
        /// <summary>
        /// Gets the certificate to use when checking the signature of a token
        /// </summary>
        public X509Certificate2 SigningCertificate { get; }

        /// <summary>
        /// Gets the certificate to use when decrypting a token
        /// </summary>
        public X509Certificate2 EncryptionCertificate { get; }

        /// <summary>
        /// Gets the certificate to use with our HTTPS connections
        /// </summary>
        public X509Certificate2 ConnectionCertificate { get; }

        /// <summary>
        /// Gets the host URL for our server
        /// </summary>
        public Uri ServerUrl { get; }

        /// <summary> 
        /// The issuer we expect to see on tokens presented to use for verification
        /// </summary> 
        public string Audience { get; }

        /// <summary>
        /// The issuer we expect to see on tokens presented to us for verification
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// The server will automatically shut down after processing one request
        /// </summary>
        public bool ExitAfterRequest { get; }

        /// <summary>
        /// Initialize a blank set of server options
        /// </summary>
        public ServerOptions()
        {
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified signing certificate
        /// </summary>
        /// <param name="certificate">Signing certificate to use (may be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithSigningCertificate(X509Certificate2 certificate)
        {
            return new ServerOptions(this, signingCertificate: Specify.As(certificate));
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified encryption certificate
        /// </summary>
        /// <param name="certificate">Encryption certificate to use (may be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithEncryptionCertificate(X509Certificate2 certificate)
        {
            return new ServerOptions(this, encryptionCertificate: Specify.As(certificate));
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified connection certificate
        /// </summary>
        /// <param name="certificate">Signing certificate to use (may not be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithConnectionCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return new ServerOptions(this, connectionCertificate: Specify.As(certificate));
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified server URL
        /// </summary>
        /// <param name="url">Hosting URL to use (may not be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithServerUrl(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            return new ServerOptions(this, serverUrl: Specify.As(url));
        }

        /// <summary> 
        /// Specify the audience expected in the token  
        /// </summary> 
        /// <param name="audience">The audience expected within the generated token.</param> 
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithAudience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("Expect to have an audience", nameof(audience));
            }

            return new ServerOptions(this, audience: Specify.As(audience));
        }

        /// <summary> 
        /// Specify the issuer expected in the token  
        /// </summary> 
        /// <param name="issuer">The audience expected within the generated token.</param> 
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithIssuer(string issuer)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Expect to have an issuer", nameof(issuer));
            }

            return new ServerOptions(this, issuer: Specify.As(issuer));
        }

        /// <summary>
        /// Indicate whether the server should automatically shut down after processing one request
        /// </summary>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithAutomaticExitAfterOneRequest(bool exitAfterRequest)
        {
            return new ServerOptions(this, exitAfterRequest: Specify.As(exitAfterRequest));
        }

        /// <summary>
        /// Mutating constructor used to create variations of an existing set of options
        /// </summary>
        /// <param name="original">Original server options to copy.</param>
        /// <param name="signingCertificate">Certificate to use for signing tokens (optional).</param>
        /// <param name="encryptionCertificate">Certificate to use for encrypting tokens (optional).</param>
        /// <param name="connectionCertificate">Certificate to use for our HTTPS connection (optional).</param>
        /// <param name="serverUrl">Server host URL (optional).</param>
        /// <param name="audience">Audience we expect to find in each token.</param>
        /// <param name="issuer">Issuer we expect to find in each token.</param>
        /// <param name="exitAfterRequest">Specify whether to automatically shut down after one request.</param>
        private ServerOptions(
            ServerOptions original,
            Specifiable<X509Certificate2> signingCertificate = default,
            Specifiable<X509Certificate2> encryptionCertificate = default,
            Specifiable<X509Certificate2> connectionCertificate = default,
            Specifiable<Uri> serverUrl = default,
            Specifiable<string> audience = default,
            Specifiable<string> issuer = default,
            Specifiable<bool> exitAfterRequest = default)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            SigningCertificate = signingCertificate.OrDefault(original.SigningCertificate);
            EncryptionCertificate = encryptionCertificate.OrDefault(original.EncryptionCertificate);
            ConnectionCertificate = connectionCertificate.OrDefault(original.ConnectionCertificate);
            ServerUrl = serverUrl.OrDefault(original.ServerUrl);
            Audience = audience.OrDefault(original.Audience);
            Issuer = issuer.OrDefault(original.Issuer);
            ExitAfterRequest = exitAfterRequest.OrDefault(original.ExitAfterRequest);
        }
    }
}
