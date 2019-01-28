using FluentAssertions.Execution;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public static class ResultTestingExtensions
    {
        public static TOk AssertOk<TOk, TError>(
            this Result<TOk, TError> result,
            string messageIfError = "Result expected to be in OK state") =>
            result.OnError(_ => Throw<TOk>(messageIfError)).Merge();

        public static TError AssertError<TOk, TError>(
            this Result<TOk, TError> result,
            string messageIfOk = "Result expected to be in error state") =>
            result.OnOk(_ => Throw<TError>(messageIfOk)).Merge();

        private static T Throw<T>(string message) => throw new AssertionFailedException(message);
    }
}
