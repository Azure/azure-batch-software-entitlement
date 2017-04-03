using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class LoggerExtensionsTests
    {
        public class LogTable : LoggerExtensionsTests
        {
            private readonly CollectingLogger _logger = new CollectingLogger();

            [Fact]
            public void GivenOneColumn_LogsExpectedRows()
            {
                var lines = new List<string>
                {
                    "alpha",
                    "beta",
                    "gamma",
                    "delta"
                };
                _logger.LogTable(LogLevel.Information, lines);
                _logger.Events.Should().HaveCount(4);
                _logger.Events.Should().OnlyContain(e => e.Level == LogLevel.Information);
                _logger.Events.Select(e => e.Message).Should().ContainInOrder(lines);
            }

            [Fact]
            public void GivenTwoColumns_LogsExpectedRows()
            {
                var lines = new List<string>
                {
                    "alpha\tMercury",
                    "beta\tVenus",
                    "gamma\tEarth",
                    "delta\tMars",
                    "epsilon\tJupiter",
                    "sigma\tSaturn",
                    "phi\tUranus",
                    "omicron\tNeptune"
                };
                _logger.LogTable(LogLevel.Information, lines);
                _logger.Events.Should().HaveCount(8);
                _logger.Events.Should().OnlyContain(e => e.Level == LogLevel.Information);
                _logger.Events.Select(e => e.Message).Should().ContainInOrder(
                    "alpha     Mercury",
                    "beta      Venus",
                    "gamma     Earth",
                    "delta     Mars",
                    "epsilon   Jupiter",
                    "sigma     Saturn",
                    "phi       Uranus",
                    "omicron   Neptune");
            }

            [Fact]
            public void GivenStringsWithBlankLeadingColumns_LogsExpectedRows()
            {
                var lines = new List<string>
                {
                    "alpha\tMercury",
                    "beta\tVenus",
                    "\tEarth",
                    "delta\tMars",
                    "\tJupiter",
                    "sigma\tSaturn",
                    "phi\tUranus",
                    "omicron\tNeptune"
                };
                _logger.LogTable(LogLevel.Information, lines);
                _logger.Events.Should().HaveCount(8);
                _logger.Events.Should().OnlyContain(e => e.Level == LogLevel.Information);
                _logger.Events.Select(e => e.Message).Should().ContainInOrder(
                    "alpha     Mercury",
                    "beta      Venus",
                    "          Earth",
                    "delta     Mars",
                    "          Jupiter",
                    "sigma     Saturn",
                    "phi       Uranus",
                    "omicron   Neptune");
            }
        }
    }
}
