using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A set of strings which can be used for the error type in <see cref="Result{TOk,TError}"/>
    /// </summary>
    public class ErrorSet : ICombinable<ErrorSet>, IEnumerable<string>
    {
        private readonly ImmutableHashSet<string> _errors;

        private ErrorSet(ImmutableHashSet<string> errors)
        {
            _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public static ErrorSet Create(IEnumerable<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            var errorHashSet = errors.ToImmutableHashSet();
            if (!errorHashSet.Any())
            {
                throw new ArgumentException("At least one error must be specified", nameof(errors));
            }

            return new ErrorSet(errorHashSet);
        }

        public static ErrorSet Create(string error, params string[] errors)
        {
            if (string.IsNullOrEmpty(error))
            {
                throw new ArgumentNullException(nameof(error));
            }

            var errorHashSet = errors != null && errors.Length > 0
                ? errors.ToImmutableHashSet().Add(error)
                : ImmutableHashSet.Create(error);

            return new ErrorSet(errorHashSet);
        }

        public static ErrorSet Empty { get; } =
            new ErrorSet(ImmutableHashSet.Create<string>());

        public ErrorSet Combine(ErrorSet combinable)
        {
            if (combinable == null)
            {
                throw new ArgumentNullException(nameof(combinable));
            }

            return new ErrorSet(_errors.Union(combinable));
        }

        public IEnumerator<string> GetEnumerator() => _errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
