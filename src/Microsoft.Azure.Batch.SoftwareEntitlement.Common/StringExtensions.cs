using System;
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

        /// <summary>
        /// Attempt to parse an Enum value from a string in a case-insensitive manner.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum</typeparam>
        /// <param name="value">The string value</param>
        /// <returns>
        /// A <see cref="Result{TOk,TError}"/> containing the enum value, or a string describing the parse error.
        /// </returns>
        public static Result<TEnum, string> ParseEnum<TEnum>(this string value) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, true, out var result))
            {
                // Successfully parsed the string
                return result;
            }

            return $"Failed to parse {value} into {typeof(TEnum).Name}";
        }
    }
}
