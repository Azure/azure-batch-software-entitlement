using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public static class ErrorableExtensions
    {
        public static T LogIfFailed<T>(this Errorable<T> errorable, ILogger logger, T valueIfFailed) =>
            errorable.OnFailure(errors =>
            {
                logger.LogErrors(errors);
                return valueIfFailed;
            });
    }
}
