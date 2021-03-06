using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extensions for working with logging
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Log a list of lines as a nicely formatted table
        /// </summary>
        /// <remarks>
        /// Each line is passed as a list of strings, one value per column.
        /// </remarks>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="lines">Lines to format.</param>
        public static void LogInformationTable(this ILogger logger, IEnumerable<IEnumerable<string>> lines)
        {
            const int columnGap = 3;
            var rows = lines.Select(r => r.ToList()).ToList();

            int ColumnWidth(int columnIndex)
            {
                return rows.Max(r => columnIndex < r.Count ? r[columnIndex].Length : 0) + columnGap;
            }

            var columnCount = rows.Max(r => r.Count);
            var widths = Enumerable.Range(0, columnCount).Select(ColumnWidth).ToList();

            foreach (var row in rows)
            {
                var s = string.Concat(row.Select((cell, index) => cell.PadRight(widths[index])))
                    .TrimEnd();
                logger.LogInformation(s);
            }
        }

        /// <summary>
        /// Log a heading 
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="heading">Heading to display.</param>
        public static void LogHeader(this ILogger logger, string heading)
        {
            var line = new string('-', heading.Length + 4);
            logger.LogInformation(line);
            logger.LogInformation("  " + heading);
            logger.LogInformation(line);
        }

        /// <summary>
        /// Log a series of errors
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="errors">Sequence of errors to log.</param>
        public static void LogErrors(this ILogger logger, IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                logger.LogError(e);
            }
        }

        /// <summary>
        /// Log a series of information messages
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="messages">Sequence of messages to log.</param>
        public static void LogInformation(this ILogger logger, IEnumerable<string> messages)
        {
            foreach (var m in messages)
            {
                logger.LogInformation(m);
            }
        }

        public static void LogJson(this ILogger logger, string json)
        {
            var pretty = JsonPrettify(json).AsLines();
            logger.LogInformation(pretty);
        }

        /// <summary>
        /// Pretty print a JSON string for logging
        /// </summary>
        /// <remarks>
        /// As found on StackOverflow: https://stackoverflow.com/a/30329731/30280
        /// </remarks>
        /// <param name="json">JSON string to reformat.</param>
        /// <returns>Equivalent JSON with nice formatting.</returns>
        private static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            {
                using (var stringWriter = new StringWriter())
                {
                    var jsonReader = new JsonTextReader(stringReader);
                    var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                    jsonWriter.WriteToken(jsonReader);
                    return stringWriter.ToString();
                }
            }
        }
    }
}
