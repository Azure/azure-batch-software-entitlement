using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Extensions for working with logging
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Log a series of lines as a table, splitting at tabs
        /// </summary>
        /// <param name="logger">Actual logger to use.</param>
        /// <param name="level">Log level for the output.</param>
        /// <param name="lines">Lines to format.</param>
        public static void LogTable(this ILogger logger, LogLevel level, IEnumerable<string> lines)
        {
            var rows = lines.Select(l => l.Split('\t')).ToList();
            var columns = rows.Max(r => r.Length);
            var widths = Enumerable.Range(0, columns)
                .Select(index => rows.Max(r => index < r.Length ? r[index].Length : 0) + 3)
                .ToList();

            string FormatRow(string[] cells, Exception exception)
            {
                return string.Join("", cells.Select((cell, index) => cell.PadRight(widths[index])))
                    .TrimEnd();
            }

            foreach (var row in rows)
            {
                logger.Log(level, (EventId)0, row, null, FormatRow);
            }
        }
    }
}
