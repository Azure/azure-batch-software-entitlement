using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Functionality for the <c>list-certificates</c> command
    /// </summary>
    public class ListCertificatesCommand : CommandBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListCertificatesCommand"/> class
        /// </summary>
        /// <param name="logger">A logger to use while executing.</param>
        public ListCertificatesCommand(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// List available certificates
        /// </summary>
        /// <param name="commandLine">Configuration from the command line.</param>
        /// <returns>Results of execution (0 = success).</returns>
        public Task<int> Execute(ListCertificatesCommandLine commandLine)
        {
            var now = DateTime.Now;

            if (commandLine.ShowExpired)
            {
                Logger.LogInformation("Including expired certificates.");
            }

            var certificateStore = new CertificateStore();
            var allCertificates = certificateStore.FindAll();
            var query = allCertificates.Where(c => c.HasPrivateKey)
                .Where(c => now < c.NotAfter || commandLine.ShowExpired)
                .ToList();
            Logger.LogInformation("Found {Count} certificates with private keys", query.Count);

            var rows = query.Select(DescribeCertificate).ToList();
            rows.Insert(0, new List<string> { "Name", "Friendly Name", "Thumbprint", "Not Before", "Not After" });

            Logger.LogInformationTable(rows);

            return Task.FromResult(0);
        }

        private static IList<string> DescribeCertificate(X509Certificate2 cert)
        {
            var status = string.Empty;
            if (cert.NotAfter < DateTime.Now)
            {
                status = "(expired)";
            }
            else if (DateTime.Now < cert.NotBefore)
            {
                status = "(not yet active)";
            }

            const string format = "HH:mm dd/mmm/yyyy";

            return new List<string>
            {
                cert.SubjectName.Name,
                cert.FriendlyName,
                cert.Thumbprint,
                cert.NotBefore.ToString(format),
                cert.NotAfter.ToString(format),
                status
            };
        }
    }
}
