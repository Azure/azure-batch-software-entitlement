using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// A container that either contains a success (OK) value, or an error.
    /// </summary>
    /// <typeparam name="TOk">The success type</typeparam>
    /// <typeparam name="TError">The error type</typeparam>
    public sealed class Result<TOk, TError>
    {
        private readonly TOk _ok;
        private readonly TError _error;
        private readonly bool _isOk;

        public Result(TOk ok)
        {
            _ok = ok;
            _error = default;
            _isOk = true;
        }

        public Result(TError error)
        {
            _ok = default;
            _error = error;
            _isOk = false;
        }

        public void Match(Action<TOk> okAction, Action<TError> errorAction)
        {
            if (okAction == null)
            {
                throw new ArgumentNullException(nameof(okAction));
            }

            if (errorAction == null)
            {
                throw new ArgumentNullException(nameof(errorAction));
            }

            if (_isOk)
            {
                okAction(_ok);
            }
            else
            {
                errorAction(_error);
            }
        }

        public T Match<T>(Func<TOk, T> fromOk, Func<TError, T> fromError)
        {
            if (fromOk == null)
            {
                throw new ArgumentNullException(nameof(fromOk));
            }

            if (fromError == null)
            {
                throw new ArgumentNullException(nameof(fromError));
            }

            return _isOk
                ? fromOk(_ok)
                : fromError(_error);
        }

        public static implicit operator Result<TOk, TError>(TError error) =>
            new Result<TOk, TError>(error);

        public static implicit operator Result<TOk, TError>(TOk ok) =>
            new Result<TOk, TError>(ok);
    }
}
