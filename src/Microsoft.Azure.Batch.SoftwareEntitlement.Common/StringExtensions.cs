using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extension methods for working with strings
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Convert a string containing multiple lines into a series of lines
        /// </summary>
        /// <param name="content">Original string to break into lines.</param>
        /// <returns>The original string, broken into sequence of lines.</returns>
        public static IEnumerable<string> AsLines(this string content)
        {
            using (var lines = new StringReader(content))
            {
                string nextLine;
                while ((nextLine = lines.ReadLine()) != null)
                {
                    yield return nextLine;
                }
            }
        }
    }
}
