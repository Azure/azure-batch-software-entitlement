using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class ResultCodes
    {
        /// <summary>
        /// Result code for successful execution
        /// </summary>
        public static readonly int Success = 0;

        /// <summary>
        /// Result code for failed execution
        /// </summary>
        public static readonly int Failed = -1;

        /// <summary>
        /// Result code for an internal error (typically an exception)
        /// </summary>
        public static readonly int InternalError = -1000;
    }
}
