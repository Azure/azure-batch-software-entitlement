using System;
using System.Security.Cryptography.X509Certificates;

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
        /// The token audience for which we will grant entitlements
        /// </summary> 
        public string Audience { get; }

        /// <summary>
        /// Initialize a blank set of server options
        /// </summary>
        public ServerOptions()
        {
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified signing certificate
        /// </summary>
        /// <param name="certificate">Signing certificate to use (may not be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithSigningCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return new ServerOptions(this, signingCertificate: certificate);
        }

        /// <summary>
        /// Create a modified <see cref="ServerOptions"/> with the specified encryption certificate
        /// </summary>
        /// <param name="certificate">Signing certificate to use (may not be null).</param>
        /// <returns>New instance of <see cref="ServerOptions"/>.</returns>
        public ServerOptions WithEncryptionCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return new ServerOptions(this, encryptionCertificate: certificate);
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

            return new ServerOptions(this, connectionCertificate: certificate);
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

            return new ServerOptions(this, serverUrl: url);
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

            return new ServerOptions(this, audience: audience);
        }

        /// <summary>
        /// Mutating constructor used to create variations of an existing set of options
        /// </summary>
        /// <param name="original">Original server options to copy.</param>
        /// <param name="signingCertificate">Certificate to use for signing tokens (optional).</param>
        /// <param name="encryptionCertificate">Certificate to use for encrypting tokens (optional).</param>
        /// <param name="connectionCertificate">Certificate to use for our HTTPS connection (optional).</param>
        /// <param name="serverUrl">Server host URL (optional).</param>
        /// <param name="audience">Audience expected of tokens.</param>
        private ServerOptions(
            ServerOptions original,
            X509Certificate2 signingCertificate = null,
            X509Certificate2 encryptionCertificate = null,
            X509Certificate2 connectionCertificate = null,
            Uri serverUrl = null,
            string audience = null)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            SigningCertificate = signingCertificate ?? original.SigningCertificate;
            EncryptionCertificate = encryptionCertificate ?? original.EncryptionCertificate;
            ConnectionCertificate = connectionCertificate ?? original.ConnectionCertificate;
            ServerUrl = serverUrl ?? original.ServerUrl;
            Audience = audience ?? original.Audience;
        }
    }
}