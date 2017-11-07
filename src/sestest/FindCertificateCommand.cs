using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Functionality for the <c>find-certificate</c> command
    /// </summary>
    public class FindCertificateCommand : CommandBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FindCertificateCommand"/> class
        /// </summary>
        /// <param name="logger">A logger to use while executing.</param>
        public FindCertificateCommand(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Find a certificate
        /// </summary>
        /// <param name="commandLine">Configuration from the command line.</param>
        /// <returns>Results of execution (0 = success).</returns>
        public int Execute(FindCertificateCommandLine commandLine)
        {
            var thumbprint = new CertificateThumbprint(commandLine.Thumbprint);
            var certificateStore = new CertificateStore();
            var certificate = certificateStore.FindByThumbprint("required", thumbprint);
            return certificate.Match(ShowCertificate, LogErrors);
        }

        private int ShowCertificate(X509Certificate2 certificate)
        {
            foreach (var line in certificate.ToString().AsLines())
            {
                Logger.LogInformation(line);
            }

            return 0;
        }
    }
}
