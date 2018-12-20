using FluentAssertions.Execution;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common.Tests
{
    public static class ResultTestingExtensions
    {
        public static TError GetError<TOk, TError>(this Result<TOk, TError> result) =>
            result.Match(
                ok => throw new AssertionFailedException("Expected an error value"),
                error => error);

        public static TOk GetOk<TOk, TError>(this Result<TOk, TError> result) =>
            result.Match(
                ok => ok,
                error => throw new AssertionFailedException("Expected an OK value"));

        public static bool IsError<TOk, TError>(this Result<TOk, TError> result) =>
            result.Match(ok => false, error => true);

        public static bool IsOk<TOk, TError>(this Result<TOk, TError> result) =>
            result.Match(ok => true, error => false);
    }
}
