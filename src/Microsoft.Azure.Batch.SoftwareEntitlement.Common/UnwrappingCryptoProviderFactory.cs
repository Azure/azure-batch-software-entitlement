using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Common
{
    /// <summary>
    /// Variation of a standard <see cref="CryptoProviderFactory"/> that unwraps keys 
    /// </summary>
    /// <remarks>It's unclear whether this is a bug in the underlying CryptoProviderFactory, but 
    /// this seems to work around the issue.</remarks>
    public class UnwrappingCryptoProviderFactory: CryptoProviderFactory
    {
        public override KeyWrapProvider CreateKeyWrapProvider(SecurityKey key, string algorithm)
        {
            switch (key)
            {
                case X509SecurityKey x509Key:
                    return new RsaKeyWrapProvider(x509Key, algorithm, true);

                default:
                    return base.CreateKeyWrapProvider(key, algorithm);
            }
        }
    }
}
