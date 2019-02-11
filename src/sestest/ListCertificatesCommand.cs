using System;
using System.Collections.Generic;
using System.Globalization;
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
            var showCertsResult = TryParseShow(commandLine.Show);

            var exitCode = showCertsResult
                .OnOk(Execute)
                .Merge(errors =>
                {
                    Logger.LogErrors(errors);
                    return ResultCodes.Failed;
                });

            return Task.FromResult(exitCode);
        }

        private int Execute(ShowCertificates showCerts)
        {
            var now = DateTime.Now;

            var certificateStore = new CertificateStore();
            var candidates = certificateStore.FindAll()
                .Where(c => c.HasPrivateKey && c.RawData != null)
                .ToList();
            Logger.LogInformation("Found {Count} certificates with private keys", candidates.Count);

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForEncrypting
                || showCerts == ShowCertificates.ForSigning
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow both encryption and signing",
                    candidates.Where(c => now < c.NotAfter
                                     && c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && c.SupportsUse(X509KeyUsageFlags.DataEncipherment)));
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForSigning
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow signing but not encryption",
                    candidates.Where(c => now < c.NotAfter
                                     && c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && !c.SupportsUse(X509KeyUsageFlags.DataEncipherment)));
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForEncrypting
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow encryption but not signing",
                    candidates.Where(c => now < c.NotAfter
                                     && !c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && c.SupportsUse(X509KeyUsageFlags.DataEncipherment)));
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow neither encryption nor signing",
                    candidates.Where(c => now < c.NotAfter
                                     && !c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && !c.SupportsUse(X509KeyUsageFlags.DataEncipherment)));
            }

            if (showCerts == ShowCertificates.All)
            {
                LogCertificates(
                    "Found {0} expired certificates",
                    candidates.Where(c => now >= c.NotAfter));
            }

            return 0;
        }

        private void LogCertificates(string title, IEnumerable<X509Certificate2> certificates)
        {
            var rows = certificates.Select(DescribeCertificate).ToList();

            if (rows.Count == 0)
            {
                // Nothing to log
                return;
            }

            rows.Insert(0, new List<string>
            {
                "Name",
                "Friendly Name",
                "Thumbprint",
                "Not Before",
                "Not After"
            });

            Logger.LogInformation("");
            Logger.LogInformation(title, rows.Count - 1);
            Logger.LogInformation("");
            Logger.LogInformationTable(rows);
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

            const string dateFormat = "s";

            return new List<string>
            {
                cert.SubjectName.Name,
                cert.FriendlyName,
                cert.Thumbprint,
                cert.NotBefore.ToString(dateFormat, CultureInfo.InvariantCulture),
                cert.NotAfter.ToString(dateFormat, CultureInfo.InvariantCulture),
                status
            };
        }

        private static Result<ShowCertificates, ErrorSet> TryParseShow(string show)
        {
            if (string.IsNullOrEmpty(show))
            {
                return ShowCertificates.NonExpired;
            }

            if (Enum.TryParse<ShowCertificates>(show, true, out var result))
            {
                // Successfully parsed the string
                return result;
            }

            return ErrorSet.Create(
                $"Failed to recognize '{show}'; valid choices are: `nonexpired` (default), 'forsigning', 'forencrypting', 'expired', and 'all'.");
        }

        [Flags]
        private enum ShowCertificates
        {
            NonExpired = 1,
            ForSigning = 2,
            ForEncrypting = 4,
            Expired = 8,
            All = NonExpired | Expired | ForSigning | ForEncrypting
        }
    }
}
