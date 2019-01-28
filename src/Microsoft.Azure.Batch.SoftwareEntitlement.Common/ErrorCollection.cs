using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A collection of strings which can be used for the error type in <see cref="Result{TOk,TError}"/>
    /// </summary>
    public class ErrorCollection : ICombinable<ErrorCollection>, IEnumerable<string>
    {
        private readonly ImmutableHashSet<string> _errors;

        private ErrorCollection(ImmutableHashSet<string> errors)
        {
            _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public static ErrorCollection Create(IEnumerable<string> errors)
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

            return new ErrorCollection(errorHashSet);
        }

        public static ErrorCollection Create(string error, params string[] errors)
        {
            if (string.IsNullOrEmpty(error))
            {
                throw new ArgumentNullException(nameof(error));
            }

            var errorHashSet = errors != null && errors.Length > 0
                ? errors.ToImmutableHashSet().Add(error)
                : ImmutableHashSet.Create(error);

            return new ErrorCollection(errorHashSet);
        }

        public static ErrorCollection Empty { get; } =
            new ErrorCollection(ImmutableHashSet.Create<string>());

        public ErrorCollection Combine(ErrorCollection combinable)
        {
            if (combinable == null)
            {
                throw new ArgumentNullException(nameof(combinable));
            }

            return new ErrorCollection(_errors.Union(combinable));
        }

        public IEnumerator<string> GetEnumerator() => _errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
