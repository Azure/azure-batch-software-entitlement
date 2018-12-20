using System.Collections.Generic;
using System.Linq;
using FluentAssertions.Execution;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Tests
{
    public static class ErrorableTestingExtensions
    {
        public static IEnumerable<string> GetErrors<T>(this Errorable<T> errorable) =>
            errorable.OnOk(t => Enumerable.Empty<string>()).Merge(errors => errors);

        public static T GetValue<T>(this Errorable<T> errorable) =>
            errorable.Merge(errors => throw new AssertionFailedException("Errorable was in a failure state"));

        public static bool HasValue<T>(this Errorable<T> errorable) =>
            errorable.OnOk(t => true).Merge(_ => false);
    }
}
