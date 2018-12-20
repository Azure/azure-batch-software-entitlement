using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class ResultExtensions
    {
        public static T LogIfFailed<T>(this Result<T, ErrorCollection> result, ILogger logger, T valueIfFailed) =>
            result.Merge(errors =>
            {
                logger.LogErrors(errors);
                return valueIfFailed;
            });
    }
}
