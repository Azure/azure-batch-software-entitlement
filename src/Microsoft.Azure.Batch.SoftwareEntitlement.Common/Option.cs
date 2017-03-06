using System;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Implementation of the Maybe monad
    /// </summary>
    public abstract class Option<T>
    {
        /// <summary>
        /// Indicate whether we have a value
        /// </summary>
        public abstract bool HasValue { get; }

        /// <summary>
        /// Create an option wrapping a value
        /// </summary>
        /// <param name="value">Value to wrap in the option</param>
        /// <returns>An <see cref="SomeOption"/> containing the value.</returns>
        public static Option<T> Some(T value)
        {
            return new SomeOption(value);
        }

        /// <summary>
        /// Create an option without a value
        /// </summary>
        /// <returns>A <see cref="NoneOption"/> with no value.</returns>
        public static Option<T> None()
        {
            return new NoneOption();
        }

        /// <summary>
        /// Apply an action based on whether we have a value or not
        /// </summary>
        /// <param name="whenSome">Action to apply when we have a value.</param>
        /// <param name="whenNone">Action to apply when we have no value.</param>
        public abstract void Apply(Action<T> whenSome, Action whenNone);

        /// <summary>
        /// Apply a function based on whether we have a value or not
        /// </summary>
        /// <typeparam name="R">Type of result to return</typeparam>
        /// <param name="whenSome">Function to use when we have a value.</param>
        /// <param name="whenNone">Function to use when we have no value.</param>
        /// <returns>Result of applying one of the functions.</returns>
        public abstract R Apply<R>(Func<T, R> whenSome, Func<R> whenNone);

        /// <summary>
        /// Implementation of an option containing a value
        /// </summary>
        private class SomeOption : Option<T>
        {
            // The value we wrap
            private readonly T _value;

            /// <summary>
            /// Gets a value indicating that we have a value
            /// </summary>
            public override bool HasValue => true;

            /// <summary>
            /// Initialize a new <see cref="Option{T}"/> with a value
            /// </summary>
            /// <param name="value"></param>
            public SomeOption(T value)
            {
                _value = value;
            }

            /// <summary>
            /// Apply an action with our value
            /// </summary>
            /// <param name="whenSome">Action to apply.</param>
            /// <param name="whenNone">Action to ignore.</param>
            public override void Apply(Action<T> whenSome, Action whenNone)
            {
                whenSome(_value);
            }

            /// <summary>
            /// Apply a function based on whether we have a value or not
            /// </summary>
            /// <typeparam name="R">Type of result to return</typeparam>
            /// <param name="whenSome">Function to use when we have a value.</param>
            /// <param name="whenNone">Function to use when we have no value.</param>
            /// <returns>Result of applying one of the functions.</returns>
            public override R Apply<R>(Func<T, R> whenSome, Func<R> whenNone) 
                => whenSome(_value);
        }

        /// <summary>
        /// Implementation of an option that does not contain a value
        /// </summary>
        private class NoneOption : Option<T>
        {
            /// <summary>
            /// Gets a value indicating that we have no value
            /// </summary>
            public override bool HasValue => false;

            /// <summary>
            /// Initialize a new <see cref="Option{T}"/> with no value
            /// </summary>
            public NoneOption()
            {
            }

            /// <summary>
            /// Apply an action with our lack of a value
            /// </summary>
            /// <param name="whenSome">Action to ignore.</param>
            /// <param name="whenNone">Action to apply.</param>
            public override void Apply(Action<T> whenSome, Action whenNone)
            {
                whenNone();
            }

            /// <summary>
            /// Apply a function based on whether we have a value or not
            /// </summary>
            /// <typeparam name="R">Type of result to return</typeparam>
            /// <param name="whenSome">Function to use when we have a value.</param>
            /// <param name="whenNone">Function to use when we have no value.</param>
            /// <returns>Result of applying one of the functions.</returns>
            public override R Apply<R>(Func<T, R> whenSome, Func<R> whenNone)
                => whenNone();
        }
    }
}
