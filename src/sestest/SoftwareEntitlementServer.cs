using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Runs a local software entitlement server
    /// </summary>
    public sealed class SoftwareEntitlementServer
    {
        // Reference to the options that configure our operation
        private readonly ServerOptions _options;

        // Logger used for diagnostics
        private readonly ILogger _logger;

        // Provider used to make more loggers
        private readonly ILoggerProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementServer"/> class
        /// </summary>
        /// <param name="options">Options to control our behavior.</param>
        /// <param name="provider">Provider required by ASP.NET.</param>
        public SoftwareEntitlementServer(ServerOptions options, ILoggerProvider provider)
        {
            _options = options;
            _provider = provider;
            _logger = _provider.CreateLogger(nameof(SoftwareEntitlementServer));
        }

        /// <summary>
        /// Run our local software entitlement server
        /// </summary>
        public void Run()
        {
            var contentDirectory = FindContentDirectory();
            var host = new WebHostBuilder()
                .ConfigureServices(ConfigureServices)
                .UseKestrel(ConfigureKestrel)
                .UseContentRoot(contentDirectory.FullName)
                .UseStartup<Startup>()
                .UseUrls(_options.ServerUrl.ToString())
                .Build();

            // This sends output directly to the console which is a bit naff
            // but avoiding it would probably be brittle.
            host.Run();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var signingKey = CreateX509SecurityKey(_options.SigningCertificate, "signing");
            var encryptingKey = CreateRsaSecurityKey(_options.EncryptionCertificate, "encryption");

            _logger.LogDebug(
                "Expected audience for all tokens {Audience}",
                _options.Audience);

            var controllerOptions = new SoftwareEntitlementsController.Options(signingKey, encryptingKey, _options.Audience);
            services.AddSingleton(controllerOptions);
            services.AddSingleton(_logger);
            services.AddSingleton(_provider);
        }

        private void ConfigureKestrel(KestrelServerOptions options)
        {
            var httpsOptions = new HttpsConnectionFilterOptions()
            {
                CheckCertificateRevocation = true,
                ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                ServerCertificate = _options.ConnectionCertificate
            };

            options.UseHttps(httpsOptions);
        }

        /// <summary>
        /// Find our content directory for static content
        /// </summary>
        /// <remarks>Does not include the "wwwroot" part of the path.</remarks>
        /// <returns>Information about the directory to use.</returns>
        private static DirectoryInfo FindContentDirectory()
        {
            var hostAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var hostFileInfo = new FileInfo(hostAssembly.Location);
            return hostFileInfo.Directory;
        }

        private RsaSecurityKey CreateRsaSecurityKey(X509Certificate2 certificate, string purpose)
        {
            if (certificate == null)
            {
                _logger.LogDebug("No certificate specified for {Purpose}", purpose);
                return null;
            }

            if (!certificate.HasPrivateKey)
            {
                _logger.LogDebug("Private key for {Certificate} is not available", certificate.Thumbprint);
                return null;
            }

            _logger.LogDebug("Creating security key for {Purpose}", purpose);
            var parameters = certificate.GetRSAPrivateKey().ExportParameters(includePrivateParameters: true);
            return new RsaSecurityKey(parameters);
        }

        private X509SecurityKey CreateX509SecurityKey(X509Certificate2 certificate, string purpose)
        {
            if (certificate == null)
            {
                _logger.LogDebug("No certificate specified for {Purpose}", purpose);
                return null;
            }

            if (!certificate.HasPrivateKey)
            {
                _logger.LogDebug("Private key for {Certificate} is not available", certificate.Thumbprint);
                return null;
            }

            _logger.LogDebug("Creating security key for {Purpose}", purpose);
            return new X509SecurityKey(certificate);
        }
    }
}
