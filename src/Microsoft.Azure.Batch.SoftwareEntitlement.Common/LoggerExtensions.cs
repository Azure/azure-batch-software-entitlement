using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

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
        /// <param name="level">Log level for the output.</param>
        /// <param name="lines">Lines to format.</param>
        public static void LogTable(this ILogger logger, LogLevel level, IEnumerable<IEnumerable<string>> lines)
        {
            const int columnGap = 3;
            var rows = lines.Select(r => r.ToList()).ToList();

            int ColumnWidth(int columnIndex)
            {
                return rows.Max(r => columnIndex < r.Count ? r[columnIndex].Length : 0) + columnGap;
            }

            var columnCount = rows.Max(r => r.Count);
            var widths = Enumerable.Range(0, columnCount).Select(ColumnWidth).ToList();

            string FormatRow(IEnumerable<string> cells)
            {
                return string.Concat(cells.Select((cell, index) => cell.PadRight(widths[index])))
                    .TrimEnd();
            }

            foreach (var row in rows)
            {
                logger.Log(level, (EventId)0, row, null, (cells, ex) => FormatRow(cells));
            }
        }

        /// <summary>
        /// Log a heading 
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="heading">Heading to display.</param>
        /// <param name="level">Log level for the output.</param>
        public static void LogHeader(this ILogger logger, string heading, LogLevel level = LogLevel.Information)
        {
            var line = new string('-', heading.Length + 4);
            logger.Log(level, (EventId)0, line, null, (s, ex) => s);
            logger.Log(level, (EventId)0, "  " + heading, null, (s, ex) => s);
            logger.Log(level, (EventId)0, line, null, (s, ex) => s);
        }

        /// <summary>
        /// Log a series of errors
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="errors">Sequence of errors to log.</param>
        public  static void LogErrors(this ILogger logger, IEnumerable<string> errors)
        {
            foreach (var e in errors)
            {
                logger.LogError(e);
            }
        }
    }
}
