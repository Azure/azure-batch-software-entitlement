namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Values used to define the claims used in our software entitlement token
    /// </summary>
    public static class Claims
    {
        /// <summary>
        /// The identifier to use for application entitlement claims
        /// </summary>
        public const string Application = "app";

        /// <summary>
        /// The default audience for each software entitlement token (essentially we self sign)
        /// </summary>
        /// <remarks>In production, the audience for each token will be the batch account for whom 
        /// it is issued.</remarks>
        public const string DefaultAudience = "https://batch.azure.test/software-entitlement";

        /// <summary>
        /// The identifier to use for the actual entitlement id
        /// </summary>
        public const string EntitlementId = "id";

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

        /// <summary>
        /// The identifier to use for the maximum expected number of CPU cores permitted on the
        /// virtual machine
        /// </summary>
        public const string CpuCoreCount = "maxcores";

        /// <summary>
        /// The unique identifier of the batch account that owns the pool
        /// </summary>
        public const string BatchAccountId = "batchaccount";

        /// <summary>
        /// The unique identifier for the pool on which the application is expected to be running
        /// </summary>
        public const string PoolId = "poolid";

        /// <summary>
        /// A unique identifier for the job within which the task is running
        /// </summary>
        public const string JobId = "jobid";

        /// <summary>
        /// A unique identifier for the task itself
        /// </summary>
        public const string TaskId = "taskid";
    }
}
