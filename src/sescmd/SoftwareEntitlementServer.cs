using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Runs a local software entitlement server
    /// </summary>
    public class SoftwareEntitlementServer
    {
        // Reference to a logger for output of activity and diagnostics
        private readonly ValidationLogger _logger;

        // Reference to a checker used to sanitize our configuration
        private readonly ServerOptionChecker _checker;

        // Store from which to load certificates
        private readonly CertificateStore _certificateStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementServer"/> class
        /// </summary>
        /// <param name="options">Options to control our behavior.</param>
        /// <param name="logger">Logger to use for output.</param>
        public SoftwareEntitlementServer(ServerOptions options, ILogger logger)
        {
            _logger = new ValidationLogger(logger);

            _checker = new ServerOptionChecker(options, _logger);
            _certificateStore = new CertificateStore(_logger);
        }

        /// <summary>
        /// Run our local software entitlement server
        /// </summary>
        public void Run()
        {
            if (!_checker.IsFullyConfigured)
            {
                // Not fully configured; errors have already been logged
                return;
            }

            var contentDirectory = FindContentDirectory();
            var builder = new WebHostBuilder()
                .UseKestrel(ConfigureKestrel)
                .UseContentRoot(contentDirectory.FullName)
                .UseStartup<Startup>()
                .UseUrls(_checker.ServerUrl.ToString());

            var host = builder.Build();

            // Only run if we initialized without errors
            if (!_logger.HasErrors)
            {
                // This sends output directly to the console which is a bit naff
                // but avoiding it would probably be brittle.
                host.Run();
            }
        }

        /// <summary>
        /// Configure the kestrel standalone server
        /// </summary>
        /// <param name="options">Options for configuration.</param>
        private void ConfigureKestrel(KestrelServerOptions options)
        {
            var connectionCertificate = FindCertificate("connection", _checker.ConnectionThumbprint);
            if (connectionCertificate != null)
            {
                var httpsOptions = new HttpsConnectionFilterOptions()
                {
                    CheckCertificateRevocation = true,
                    ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                    ServerCertificate = connectionCertificate
                };

                options.UseHttps(httpsOptions);
            }
        }

        /// <summary>
        /// Find a certificate from a thumbprint
        /// </summary>
        /// <param name="kind">Kind of certificate we need (used for logging).</param>
        /// <param name="thumbprint">Thumbprint of the desired certificate.</param>
        /// <returns></returns>
        private X509Certificate2 FindCertificate(string kind, CertificateThumbprint thumbprint)
        {
            var connectionCertificate = _certificateStore.FindByThumbprint(_checker.ConnectionThumbprint);
            if (connectionCertificate == null)
            {
                _logger.LogError($"Failed to find {kind} certificate for thumbprint '{thumbprint}'");
            }

            return connectionCertificate;
        }

        /// <summary>
        /// Find our content directory for static content
        /// </summary>
        /// <remarks>Does not include the wwwroot part of the path.</remarks>
        /// <returns>Information about the directory to use.</returns>
        private static DirectoryInfo FindContentDirectory()
        {
            var hostAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var hostFileInfo = new FileInfo(hostAssembly.Location);
            var hostDirectory = hostFileInfo.Directory;
            return hostDirectory;
        }
    }
}
