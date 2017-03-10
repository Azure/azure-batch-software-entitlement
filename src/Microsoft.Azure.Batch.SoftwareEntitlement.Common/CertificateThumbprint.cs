using System;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Represents the thumbprint of a certificate as a semantic type
    /// </summary>
    public sealed class CertificateThumbprint : IEquatable<CertificateThumbprint>
    {
        // The actual thumbprint we wrap
        private readonly string _thumbprint;

        /// <summary>
        /// Initializes a new instance of the CertificateThumbprint class
        /// </summary>
        /// <param name="thumbprint">Thumbprint to wrap</param>
        public CertificateThumbprint(string thumbprint)
        {
            _thumbprint = SanitizeThumbprint(thumbprint);
        }

        /// <summary>
        /// Is this thumbprint equal to another thumbprint
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(CertificateThumbprint other)
        {
            if (other == null)
            {
                // Not equal to null
                return false;
            }

            return string.Equals(_thumbprint, other._thumbprint, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Does this thumbprint match the thumbprint from the specified string?
        /// </summary>
        /// <remarks>Formatting of the thumbprints (e.g. whitespace) is ignored.</remarks>
        /// <param name="thumbprint">String to compare</param>
        /// <returns>True if the thumbprints match, false otherwise.</returns>
        public bool HasThumbprint(string thumbprint)
        {
            var t = SanitizeThumbprint(thumbprint ?? string.Empty);
            return string.Equals(_thumbprint, t, StringComparison.OrdinalIgnoreCase);
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
        private string SanitizeThumbprint(string thumbprint)
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
