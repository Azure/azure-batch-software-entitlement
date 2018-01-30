using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public abstract class UnpackingExceptionLoggerTests
    {
        public class Constructor : UnpackingExceptionLoggerTests
        {
            [Fact]
            public void GivenNoInnerLogger_ThrowsArgumentNullException()
            {
                var exception =
                    Assert.Throws<ArgumentNullException>(
                        () => new UnpackingExceptionLogger(null));
                exception.ParamName.Should().Be("innerLogger");
            }
        }

        public class LogMethod : UnpackingExceptionLoggerTests
        {
            private readonly CollectingLogger _collectingLogger = new CollectingLogger();

            private readonly UnpackingExceptionLogger _unpackingExceptionLogger;

            private readonly EventId _eventId = new EventId(42, "Universal");

            public LogMethod()
            {
                _unpackingExceptionLogger = new UnpackingExceptionLogger(_collectingLogger);
            }

            [Theory]
            [InlineData(LogLevel.Error)]
            [InlineData(LogLevel.Warning)]
            [InlineData(LogLevel.Information)]
            [InlineData(LogLevel.Debug)]
            public void WhenLoggingAtLevel_CollectsEventAtLevel(LogLevel level)
            {
                _unpackingExceptionLogger.Log(level, _eventId, "State", null, (s, e) => s);
                _collectingLogger.Events
                    .Where(e => e.Level == level)
                    .Should()
                    .HaveCount(1);
            }

            [Theory]
            [MemberData(nameof(TestCasesForExceptionLogging))]
            public void WhenLoggingException_CollectsExpectedErrorCount(Exception exception, int errorCount)
            {
                _unpackingExceptionLogger.Log(LogLevel.Information, _eventId, "Logging an exception now", exception, (s, e) => s);
                _collectingLogger.Events
                    .Where(e => e.Level == LogLevel.Error)
                    .Should()
                    .HaveCount(errorCount);
            }

            [Fact]
            public void WhenLoggingException_CollectsStackTraceDebugMessages()
            {
                try
                {
                    throw new InvalidOperationException("Bang!");
                }
                catch (Exception exception)
                {
                    _unpackingExceptionLogger.Log(LogLevel.Critical, _eventId, "Kaboom", exception, (s, e) => s);
                }

                _collectingLogger.Events
                    .Count(e => e.Level == LogLevel.Debug)
                    .Should()
                    .BeGreaterThan(0);
            }

            public static IEnumerable<object[]> TestCasesForExceptionLogging()
            {
                // Single simple exception
                var invalidOperationException = new InvalidOperationException();
                yield return new object[] { invalidOperationException, 1 };

                // Another single exception, different type
                var argumentException = new ArgumentException("arg");
                yield return new object[] { argumentException, 1 };

                // Parent and child
                var wrappedException = new InvalidOperationException("Wrapped", invalidOperationException);
                yield return new object[] { wrappedException, 2 };

                // Parent, three children, one grandchild
                var aggregateException = new AggregateException(invalidOperationException, argumentException, wrappedException);
                yield return new object[] { aggregateException, 5 };
            }
        }
    }
}
