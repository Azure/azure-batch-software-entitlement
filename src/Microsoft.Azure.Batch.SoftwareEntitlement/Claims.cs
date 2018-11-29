namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Values used to define the claims used in our software entitlement token
    /// </summary>
    public static class Claims
    {
        /// <summary>
        /// The identifier to use for allowed application claims
        /// </summary>
        public const string Application = "app";

        /// <summary>
        /// The default audience for each software entitlement token (essentially we self sign)
        /// </summary>
        /// <remarks>In production, the audience for each token will be the batch account for whom 
        /// it is issued.</remarks>
        public const string DefaultAudience = "https://batch.azure.test/software-entitlement";

        /// <summary>
        /// The identifier to use for the token id
        /// </summary>
        public const string TokenId = "id";

        /// <summary>
        /// The identifier to use for the ip address of the entitled machine
        /// </summary>
        public const string IpAddress = "ip";

        /// <summary>
        /// The default issuer of each software entitlement token
        /// </summary>
        public const string DefaultIssuer = "https://batch.azure.test/software-entitlement";

        /// <summary>
        /// Identifier use for the claim specifying the permitted virtual machine
        /// </summary>
        public const string VirtualMachineId = "vmid";
    }
}
