using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// The result of a predicate (a boolean) in which <value>false</value> corresponds to a certain type.
    /// </summary>
    /// <typeparam name="TFailure">The type used when the predicate evaluates to <value>false</value>.</typeparam>
    public class PredicateResult<TFailure>
    {
        private readonly bool _isSuccess;
        private readonly TFailure _failure;

        private PredicateResult(bool isSuccess, TFailure failure)
        {
            _isSuccess = isSuccess;
            _failure = failure;
        }

        public static PredicateResult<TFailure> Success() =>
            new PredicateResult<TFailure>(true, default);

        public static PredicateResult<TFailure> Failure(TFailure left) =>
            new PredicateResult<TFailure>(false, left);

        /// <summary>
        /// Creates a new object from either the success or failure case.
        /// </summary>
        /// <typeparam name="T">The type of the new object.</typeparam>
        /// <param name="onSuccess">Returns the new object.</param>
        /// <param name="onFailure">Creates the new object from the failure object.</param>
        /// <returns>The new object</returns>
        public T Match<T>(Func<T> onSuccess, Func<TFailure, T> onFailure) =>
            _isSuccess ? onSuccess() : onFailure(_failure);
    }
}
