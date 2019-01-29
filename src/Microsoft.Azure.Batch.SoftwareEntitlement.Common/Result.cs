using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    public static class Result
    {
        public static Result<TOk, ErrorCollection> FromOk<TOk>(TOk ok) =>
            new Result<TOk, ErrorCollection>(ok);
    }

    /// <summary>
    /// A container that either contains a success (OK) value, or an error.
    /// </summary>
    /// <typeparam name="TOk">The success type</typeparam>
    /// <typeparam name="TError">The error type</typeparam>
    public class Result<TOk, TError>
    {
        private readonly TOk _ok;
        private readonly TError _error;
        private readonly bool _isOk;

        protected Result(TOk ok, TError error, bool isOk)
        {
            _ok = ok;
            _error = error;
            _isOk = isOk;
        }

        public Result(TOk ok) : this(ok, default, true)
        {
        }

        public Result(TError error) : this(default, error, false)
        {
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
