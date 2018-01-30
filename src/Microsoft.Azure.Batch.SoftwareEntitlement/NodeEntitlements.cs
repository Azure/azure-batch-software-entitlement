using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// The software entitlements for a specific compute node
    /// </summary>
    public class NodeEntitlements
    {
        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// The moment at which the entitlement is issued
        /// </summary>
        public DateTimeOffset IssuedAt { get; }

        /// <summary>
        /// The earliest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// The latest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// The set of applications that are entitled to run
        /// </summary>
        public ImmutableHashSet<string> Applications { get; }

        /// <summary>
        /// The IP addresses of the machine authorized to use this entitlement
        /// </summary>
        public ImmutableHashSet<IPAddress> IpAddresses { get; }

        /// <summary>
        /// The unique identifier for this entitlement
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the number of CPU cores configured for the selected VM SKU
        /// </summary>
        public int? CpuCoreCount { get; }

        /// <summary>
        /// The unique identifier of the batch account that owns the pool
        /// </summary>
        public string BatchAccountId { get; }

        /// <summary>
        /// The unique identifier for the pool on which the application is expected to be running
        /// </summary>
        public string PoolId { get; }

        /// <summary>
        /// A unique identifier for the job within which the task is running
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// A unique identifier for the task itself
        /// </summary>
        public string TaskId { get; }

        /// <summary>
        /// The audience for whom this entitlement is intended
        /// </summary>
        public string Audience { get; }

        /// <summary>
        /// The issuer who hands out entitlement tokens
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEntitlements"/> class
        /// </summary>
        public NodeEntitlements()
        {
            Applications = ImmutableHashSet<string>.Empty;
            IpAddresses = ImmutableHashSet<IPAddress>.Empty;
        }

        public static Errorable<NodeEntitlements> Build(IEntitlementPropertyProvider provider)
        {
            return Errorable.Success(new NodeEntitlements())
                .With(provider.NotBefore()).Map((e, val) => e.FromInstant(val))
                .With(provider.NotAfter()).Map((e, val) => e.UntilInstant(val))
                .With(provider.IssuedAt()).Map((e, val) => e.WithIssuedAt(val))
                .With(provider.Issuer()).Map((e, val) => e.WithIssuer(val))
                .With(provider.Audience()).Map((e, val) => e.WithAudience(val))
                .With(provider.ApplicationIds()).Map((e, vals) => e.WithApplications(vals))
                .With(provider.IpAddresses()).Map((e, vals) => e.WithIpAddresses(vals))
                .With(provider.VirtualMachineId()).Map((e, val) => e.WithVirtualMachineId(val))
                .With(provider.EntitlementId()).Map((e, val) => e.WithIdentifier(val));
        }

        /// <summary>
        /// Specify the virtual machine Id of the machine
        /// </summary>
        /// <param name="virtualMachineId">Virtual machine ID to include in the entitlement.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithVirtualMachineId(string virtualMachineId)
        {
            return new NodeEntitlements(this, virtualMachineId: Specify.As(virtualMachineId));
        }

        /// <summary>
        /// Specify the instant at which the token is issued
        /// </summary>
        /// <param name="issuedAt">Date the token is issued.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithIssuedAt(DateTimeOffset issuedAt)
        {
            return new NodeEntitlements(this, issuedAt: Specify.As(issuedAt));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements FromInstant(DateTimeOffset notBefore)
        {
            return new NodeEntitlements(this, notBefore: Specify.As(notBefore));
        }

        /// <summary>
        /// Specify an instant before which the token will not be valid
        /// </summary>
        /// <param name="notAfter">Earliest instant of availability.</param>
        /// <returns></returns>
        public NodeEntitlements UntilInstant(DateTimeOffset notAfter)
        {
            return new NodeEntitlements(this, notAfter: Specify.As(notAfter));
        }

        /// <summary>
        /// Specify the entitled applications
        /// </summary>
        /// <param name="application">Identifiers of the applications.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithApplications(params string[] applications)
        {
            if (applications == null)
            {
                throw new ArgumentNullException(nameof(applications));
            }

            return WithApplications((IEnumerable<string>)applications);
        }

        /// <summary>
        /// Specify the list of entitled applications
        /// </summary>
        /// <param name="application">Identifiers of the applications.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithApplications(IEnumerable<string> applications)
        {
            if (applications == null)
            {
                throw new ArgumentNullException(nameof(applications));
            }

            applications = applications.Select(appId => appId.Trim());
            return new NodeEntitlements(this, applications: ImmutableHashSet.CreateRange(applications));
        }

        /// <summary>
        /// Specify the IPAddresses of the entitled machine
        /// </summary>
        /// <param name="address">IP Addresses of the machine to run the entitled application(s).</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIpAddresses(params IPAddress[] ipAddresses)
        {
            if (ipAddresses == null)
            {
                throw new ArgumentNullException(nameof(ipAddresses));
            }

            return WithIpAddresses((IEnumerable<IPAddress>)ipAddresses);
        }

        /// <summary>
        /// Specify the list of IPAddresses of the entitled machine
        /// </summary>
        /// <param name="address">IP Addresses of the machine to run the entitled application(s).</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIpAddresses(IEnumerable<IPAddress> ipAddresses)
        {
            if (ipAddresses == null)
            {
                throw new ArgumentNullException(nameof(ipAddresses));
            }

            return new NodeEntitlements(this, addresses: ImmutableHashSet.CreateRange(ipAddresses));
        }

        /// <summary>
        /// Specify the entitlement Id to use
        /// </summary>
        /// <param name="identifier">The entitlement identifier to use for correlating activity.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIdentifier(string identifier)
        {
            return new NodeEntitlements(this, identifier: Specify.As(identifier));
        }

        /// <summary>
        /// Optionally specify the maximum number of CPU cores expected to be found on the machine
        /// </summary>
        /// <param name="cpuCoreCount">The number of CPU cores, or null if not required.</param>
        /// <returns>A new entitlement.</returns>
        public NodeEntitlements WithCpuCoreCount(int? cpuCoreCount)
        {
            return new NodeEntitlements(this, cpuCoreCount: Specify.As(cpuCoreCount));
        }

        /// <summary>
        /// Specify the batch account ID with which the entitlement is associated
        /// </summary>
        /// <param name="batchAccountId">The batch account ID</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithBatchAccountId(string batchAccountId)
        {
            return new NodeEntitlements(this, batchAccountId: Specify.As(batchAccountId));
        }

        /// <summary>
        /// Specify the pool ID with which the entitlement is associated
        /// </summary>
        /// <param name="poolId">The pool ID</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithPoolId(string poolId)
        {
            return new NodeEntitlements(this, poolId: Specify.As(poolId));
        }

        /// <summary>
        /// Specify the job ID with which the entitlement is associated
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithJobId(string jobId)
        {
            return new NodeEntitlements(this, jobId: Specify.As(jobId));
        }

        /// <summary>
        /// Specify the task ID with which the entitlement is associated
        /// </summary>
        /// <param name="taskId">The task ID</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithTaskId(string taskId)
        {
            return new NodeEntitlements(this, taskId: Specify.As(taskId));
        }

        /// <summary>
        /// Specify the audience to use in the token 
        /// </summary>
        /// <param name="audience">The audience for the generated token.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithAudience(string audience)
        {
            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("Expect to have an audience", nameof(audience));
            }

            return new NodeEntitlements(this, audience: Specify.As(audience));
        }

        /// <summary>
        /// Specify the issuer to use in the token 
        /// </summary>
        /// <param name="issuer">The issuer for the generated token.</param>
        /// <returns>A new entitlement</returns>
        public NodeEntitlements WithIssuer(string issuer)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("Expect to have an issuer", nameof(issuer));
            }

            return new NodeEntitlements(this, issuer: Specify.As(issuer));
        }

        /// <summary>
        /// Cloning constructor to initialize a new instance of the <see cref="NodeEntitlements"/>
        /// class as a (near) copy of an existing one.
        /// </summary>
        /// <remarks>Specify any of the optional parameters to modify the clone from the original.</remarks>
        /// <param name="original">Original entitlement to clone.</param>
        /// <param name="issuedAt">Optionally specify a new value for <see cref="IssuedAt"/></param>
        /// <param name="notBefore">Optionally specify a new value for <see cref="NotBefore"/>.</param>
        /// <param name="notAfter">Optionally specify a new value for <see cref="NotAfter"/>.</param>
        /// <param name="virtualMachineId">Optionally specify a new value for <see cref="VirtualMachineId"/>.</param>
        /// <param name="applications">The set of applications entitled to run.</param>
        /// <param name="identifier">Identifier to use for this entitlement.</param>
        /// <param name="addresses">Addresses of the entitled machine.</param>
        /// <param name="cpuCoreCount">Optionally specify a new value for <see cref="CpuCoreCount"/></param>
        /// <param name="batchAccountId">Optionally specify a new value for <see cref="BatchAccountId"/></param>
        /// <param name="poolId">Optionally specify a new value for <see cref="PoolId"/></param>
        /// <param name="jobId">Optionally specify a new value for <see cref="JobId"/></param>
        /// <param name="taskId">Optionally specify a new value for <see cref="TaskId"/></param>
        /// <param name="audience">Audience for whom the token is intended.</param>
        /// <param name="issuer">Issuer identifier for the token.</param>
        private NodeEntitlements(
            NodeEntitlements original,
            Specifiable<DateTimeOffset> issuedAt = default,
            Specifiable<DateTimeOffset> notBefore = default,
            Specifiable<DateTimeOffset> notAfter = default,
            Specifiable<string> virtualMachineId = default,
            ImmutableHashSet<string> applications = null,
            Specifiable<string> identifier = default,
            ImmutableHashSet<IPAddress> addresses = null,
            Specifiable<int?> cpuCoreCount = default,
            Specifiable<string> batchAccountId = default,
            Specifiable<string> poolId = default,
            Specifiable<string> jobId = default,
            Specifiable<string> taskId = default,
            Specifiable<string> audience = default,
            Specifiable<string> issuer = default)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            IssuedAt = issuedAt.OrDefault(original.IssuedAt);
            NotBefore = notBefore.OrDefault(original.NotBefore);
            NotAfter = notAfter.OrDefault(original.NotAfter);
            VirtualMachineId = virtualMachineId.OrDefault(original.VirtualMachineId);
            Applications = applications ?? original.Applications;
            Identifier = identifier.OrDefault(original.Identifier);
            IpAddresses = addresses ?? original.IpAddresses;
            CpuCoreCount = cpuCoreCount.OrDefault(original.CpuCoreCount);
            BatchAccountId = batchAccountId.OrDefault(original.BatchAccountId);
            PoolId = poolId.OrDefault(original.PoolId);
            JobId = jobId.OrDefault(original.JobId);
            TaskId = taskId.OrDefault(original.TaskId);
            Audience = audience.OrDefault(original.Audience);
            Issuer = issuer.OrDefault(original.Issuer);
        }
    }
}
