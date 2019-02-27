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
            var executeResult =
                from showCerts in TryParseShow(commandLine.Show)
                join extraColumns in TryParseExtraColumns(commandLine.ExtraColumns) on true equals true
                select Execute(showCerts, extraColumns.ToList());

            var exitCode = executeResult.Merge(errors =>
            {
                Logger.LogErrors(errors);
                return ResultCodes.Failed;
            });

            return Task.FromResult(exitCode);
        }

        private int Execute(ShowCertificates showCerts, IList<ExtraColumn> extraColumns)
        {
            var now = DateTime.Now;

            var certificateStore = new CertificateStore();
            var candidates = certificateStore.FindAll()
                // Need to filter on private key existence before calling Distinct() because
                // default equality checking does not take private keys into account.
                .Where(c => c.HasPrivateKey && c.RawData != null)
                .Distinct()
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
                                     && c.SupportsUse(X509KeyUsageFlags.DataEncipherment)),
                    extraColumns);
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForSigning
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow signing but not encryption",
                    candidates.Where(c => now < c.NotAfter
                                     && c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && !c.SupportsUse(X509KeyUsageFlags.DataEncipherment)),
                    extraColumns);
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForEncrypting
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow encryption but not signing",
                    candidates.Where(c => now < c.NotAfter
                                     && !c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && c.SupportsUse(X509KeyUsageFlags.DataEncipherment)),
                    extraColumns);
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.NonExpired)
            {
                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow neither encryption nor signing",
                    candidates.Where(c => now < c.NotAfter
                                     && !c.SupportsUse(X509KeyUsageFlags.DigitalSignature)
                                     && !c.SupportsUse(X509KeyUsageFlags.DataEncipherment)),
                    extraColumns);
            }

            if (showCerts == ShowCertificates.All
                || showCerts == ShowCertificates.ForServerAuth
                || showCerts == ShowCertificates.NonExpired)
            {
                var serverAuthCerts = candidates
                    .Where(cert => now < cert.NotAfter && cert.SupportsServerAuthentication())
                    .Select(cert => new
                    {
                        IsVerified = cert.Verify(),
                        Cert = cert
                    })
                    .ToList();

                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow server authentication and are verified",
                    serverAuthCerts.Where(c => c.IsVerified).Select(c => c.Cert),
                    extraColumns);

                LogCertificates(
                    "Found {0} non-expired certificates with private keys that allow server authentication and are NOT verified",
                    serverAuthCerts.Where(c => !c.IsVerified).Select(c => c.Cert),
                    extraColumns);
            }

            if (showCerts == ShowCertificates.All)
            {
                LogCertificates(
                    "Found {0} expired certificates",
                    candidates.Where(c => now >= c.NotAfter),
                    extraColumns);
            }

            return 0;
        }

        private void LogCertificates(
            string title,
            IEnumerable<X509Certificate2> certificates,
            IList<ExtraColumn> extraColumns)
        {
            var rows = certificates.Select(c => DescribeCertificate(c, extraColumns)).ToList();

            if (rows.Count == 0)
            {
                // Nothing to log
                return;
            }

            var headers = new List<string>
            {
                "DNS Name",
                "Thumbprint",
                "Not Before",
                "Not After"
            };

            foreach (var extraColumn in extraColumns)
            {
                switch (extraColumn)
                {
                    case ExtraColumn.SubjectName:
                        headers.Add("Subject Name");
                        break;
                    case ExtraColumn.FriendlyName:
                        headers.Add("Friendly Name");
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected {typeof(ExtraColumn).Name} value: {extraColumn}");
                }
            }

            rows.Insert(0, headers);

            Logger.LogInformation("");
            Logger.LogInformation(title, rows.Count - 1);
            Logger.LogInformation("");
            Logger.LogInformationTable(rows);
        }


        private static IList<string> DescribeCertificate(
            X509Certificate2 cert,
            IList<ExtraColumn> extraColumns)
        {
            const string dateFormat = "s";

            var result = new List<string>
            {
                cert.GetNameInfo(X509NameType.DnsName, forIssuer: false),
                cert.Thumbprint,
                cert.NotBefore.ToString(dateFormat, CultureInfo.InvariantCulture),
                cert.NotAfter.ToString(dateFormat, CultureInfo.InvariantCulture)
            };

            foreach (var extraColumn in extraColumns)
            {
                switch (extraColumn)
                {
                    case ExtraColumn.SubjectName:
                        result.Add(cert.SubjectName.Name);
                        break;
                    case ExtraColumn.FriendlyName:
                        result.Add(cert.FriendlyName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected {typeof(ExtraColumn).Name} value: {extraColumn}");
                }
            }

            var status = string.Empty;
            if (cert.NotAfter < DateTime.Now)
            {
                status = "(expired)";
            }
            else if (DateTime.Now < cert.NotBefore)
            {
                status = "(not yet active)";
            }

            result.Add(status);

            return result;
        }

        private static Result<ShowCertificates, ErrorSet> TryParseShow(string show)
        {
            if (string.IsNullOrEmpty(show))
            {
                return ShowCertificates.NonExpired;
            }

            return show.ParseEnum<ShowCertificates>().OnError(_ => ErrorSet.Create(
                $"Failed to recognize '{show}'; valid choices are: `nonexpired` (default), 'forsigning', 'forencrypting', 'forserverauth', 'expired', and 'all'."));
        }

        [Flags]
        private enum ShowCertificates
        {
            NonExpired = 1,
            ForSigning = 2,
            ForEncrypting = 4,
            ForServerAuth = 8,
            Expired = 16,
            All = NonExpired | Expired | ForSigning | ForEncrypting | ForServerAuth
        }

        private static Result<IEnumerable<ExtraColumn>, ErrorSet> TryParseExtraColumns(
            IEnumerable<string> extraColumnNames)
        {
            var results =
                from colName in extraColumnNames
                select colName.ParseEnum<ExtraColumn>().OnError(_ => ErrorSet.Create(
                    $"Failed to recognize '{colName}'; valid choices are: 'subjectname' and 'friendlyname'."));

            return results.Reduce();
        }

        private enum ExtraColumn
        {
            SubjectName,
            FriendlyName
        }
    }
}
