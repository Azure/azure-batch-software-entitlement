using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    public class Certificates
    {
        public X509Certificate2 FindByThumbprint(string thumbprint)
        {
            return null;
        }

        /// <summary>
        /// Filter out any unsafe characters from a potential thumbprint
        /// </summary>
        /// <remarks>When copying a thumbprint from MMC, it ends up with a zero with uncode 
        /// character (actually, a left-to-right indicator) which can get in the way of finding a 
        /// certificate. Filtering the string helps to avoid this problem. 
        /// See http://stackoverflow.com/a/14852713/30280 for more.</remarks>
        /// <param name="thumbprint">Thumbprint to sanitize.</param>
        /// <returns>Sanitized string.</returns>
        public static string SanitizeThumbprint(string thumbprint)
        {
            var builder = new StringBuilder();
            foreach (var c in thumbprint.Where(char.IsLetterOrDigit))
            {
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
