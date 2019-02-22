namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Signifies that values of implementing type <typeparamref name="T"/> can be combined with other values of that type.
    /// </summary>
    /// <typeparam name="T">The implementing type</typeparam>
    public interface ICombinable<T>
    {
        T Combine(T combinable);
    }
}
