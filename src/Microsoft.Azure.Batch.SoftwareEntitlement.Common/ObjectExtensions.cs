namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    public static class ObjectExtensions
    {
        public static Result<TOk, ErrorSet> AsOk<TOk>(this TOk value)
            => new Result<TOk, ErrorSet>(value);
    }
}
