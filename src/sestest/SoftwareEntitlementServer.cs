using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;

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
                .UseKestrel(ConfigureKestrel)
                .UseContentRoot(contentDirectory.FullName)
                .UseStartup<Startup>()
                .UseUrls(_options.ServerUrl.ToString())
                .Build();

            // This sends output directly to the console which is a bit naff
            // but avoiding it would probably be brittle.
            host.Run();
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
    }
}
