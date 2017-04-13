using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlementServer"/> class
        /// </summary>
        /// <param name="options">Options to control our behavior.</param>
        public SoftwareEntitlementServer(ServerOptions options)
        {
            _options = options;
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
            var signingKey = CreateKey(_options.SigningCertificate);
            var encryptingKey = CreateKey(_options.EncryptionCertificate);
            var controllerOptions = new SoftwareEntitlementControllerOptions(signingKey, encryptingKey);
            services.AddSingleton(controllerOptions);
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
        /// <remarks>Does not include the wwwroot part of the path.</remarks>
        /// <returns>Information about the directory to use.</returns>
        private static DirectoryInfo FindContentDirectory()
        {
            var hostAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var hostFileInfo = new FileInfo(hostAssembly.Location);
            return hostFileInfo.Directory;
        }

        private SecurityKey CreateKey(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            return new X509SecurityKey(certificate);
        }
    }
}
