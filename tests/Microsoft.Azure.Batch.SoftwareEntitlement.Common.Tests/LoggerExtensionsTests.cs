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
                var lines = new List<IList<string>>
                {
                    new List<string> {"alpha"},
                    new List<string> {"beta"},
                    new List<string> {"gamma"},
                    new List<string> {"delta"}
                };
                _logger.LogTable(LogLevel.Information, lines);
                _logger.Events.Should().HaveCount(4);
                _logger.Events.Should().OnlyContain(e => e.Level == LogLevel.Information);
                _logger.Events.Select(e => e.Message).Should().ContainInOrder("alpha", "beta", "gamma", "delta");
            }

            [Fact]
            public void GivenTwoColumns_LogsExpectedRows()
            {
                var lines = new List<IList<string>>
                {
                    new List<string> {"alpha", "Mercury"},
                    new List<string> {"beta", "Venus"},
                    new List<string> {"gamma", "Earth"},
                    new List<string> {"delta", "Mars"},
                    new List<string> {"epsilon", "Jupiter"},
                    new List<string> {"sigma", "Saturn"},
                    new List<string> {"phi", "Uranus"},
                    new List<string> {"omicron", "Neptune"}
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
                var lines = new List<IList<string>>
                {
                    new List<string> {"alpha", "Mercury"},
                    new List<string> {"beta", "Venus"},
                    new List<string> {"", "Earth"},
                    new List<string> {"delta", "Mars"},
                    new List<string> {"", "Jupiter"},
                    new List<string> {"sigma", "Saturn"},
                    new List<string> {"phi", "Uranus"},
                    new List<string> {"omicron", "Neptune"}
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
