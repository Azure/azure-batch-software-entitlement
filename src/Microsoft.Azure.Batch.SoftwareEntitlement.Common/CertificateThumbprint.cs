using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Represents the thumbprint of a certificate as a semantic type
    /// </summary>
    public struct CertificateThumbprint : IEquatable<CertificateThumbprint>
    {
        // The actual thumbprint we wrap
        private readonly string _thumbprint;

        /// <summary>
        /// Compare two thumbprints for equality
        /// </summary>
        /// <param name="left">First certificate thumbprint to test.</param>
        /// <param name="right">Second certificate thumbprint to test.</param>
        /// <returns>True if they represent the same thumbprint, false otherwise.</returns>
        public static bool operator ==(CertificateThumbprint left, CertificateThumbprint right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two thumb-prints for inequality
        /// </summary>
        /// <param name="left">First certificate thumbprint to test.</param>
        /// <param name="right">Second certificate thumbprint to test.</param>
        /// <returns>True if they represent different thumbprints, false otherwise.</returns>
        public static bool operator !=(CertificateThumbprint left, CertificateThumbprint right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Initializes a new instance of the CertificateThumbprint class
        /// </summary>
        /// <param name="thumbprint">Thumbprint to wrap</param>
        public CertificateThumbprint(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                throw new ArgumentException("No thumbprint specified", nameof(thumbprint));
            }

            _thumbprint = SanitizeThumbprint(thumbprint);
        }

        /// <summary>
        /// Return the string representation of the thumbprint
        /// </summary>
        /// <returns>The wrapped thumbprint.</returns>
        public override string ToString() => _thumbprint;

        /// <summary>
        /// Indicates whether this <see cref="CertificateThumbprint"/> and a specified object are equal.
        /// </summary>
        /// <returns>True if <paramref name="obj" /> and this thumbprint represent the same value; otherwise, false. </returns>
        /// <param name="obj">The object to compare with the current thumbprint. </param>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(CertificateThumbprint))
            {
                return false;
            }

            return Equals((CertificateThumbprint)obj);
        }

        public static bool operator ==(CertificateThumbprint left, CertificateThumbprint right)
            => left.Equals(right);

        public static bool operator !=(CertificateThumbprint left, CertificateThumbprint right)
            => !left.Equals(right);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hashcode based on the thumbprint string for this instance.</returns>
        public override int GetHashCode()
        {
            return _thumbprint.GetHashCode();
        }

        /// <summary>
        /// Is this thumbprint equal to another thumbprint
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [Pure]
        public bool Equals(CertificateThumbprint other)
        {
            return string.Equals(_thumbprint, other._thumbprint, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Does this thumbprint match the thumbprint from the specified string?
        /// </summary>
        /// <remarks>Formatting of the thumbprints (e.g. whitespace) is ignored.</remarks>
        /// <param name="thumbprint">String to compare</param>
        /// <returns>True if the thumbprints match, false otherwise.</returns>
        [Pure]
        public bool Matches(string thumbprint)
        {
            var t = SanitizeThumbprint(thumbprint ?? string.Empty);
            return string.Equals(_thumbprint, t, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Filter out any unsafe characters from a potential thumbprint
        /// </summary>
        /// <remarks>When copying a thumbprint from MMC, it ends up with a zero width unicode 
        /// character (actually, a "left-to-right indicator") which can get in the way of finding a 
        /// certificate. Filtering the string works to avoid this problem. 
        /// See http://stackoverflow.com/a/14852713/30280 for more.</remarks>
        /// <param name="thumbprint">Thumbprint to sanitize.</param>
        /// <returns>Sanitized string.</returns>
        private static string SanitizeThumbprint(string thumbprint)
        {
            var builder = new StringBuilder();
            foreach (var c in thumbprint.Where(char.IsLetterOrDigit))
            {
                builder.Append(char.ToUpperInvariant(c));
            }

            return builder.ToString();
        }
    }
}
