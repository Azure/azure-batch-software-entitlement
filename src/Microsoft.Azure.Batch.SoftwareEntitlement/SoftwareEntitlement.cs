using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    /// <summary>
    /// Definition of a software entitlement
    /// </summary>
    public class SoftwareEntitlement
    {
        // Reference to the logger we use for reporting activity
        private readonly MonitoringLogger _logger;

        // Parser used to convert from text into instants of time
        private readonly TimestampParser _timestampParser;

        /// <summary>
        /// Gets the virtual machine identifier for the machine entitled to use the specified packages
        /// </summary>
        public string VirtualMachineId { get; }

        /// <summary>
        /// The moment at which the entitlement was created
        /// </summary>
        public DateTimeOffset Created { get; }

        /// <summary>
        /// The earliest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotBefore { get; }

        /// <summary>
        /// The latest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotAfter { get; }

        /// <summary>
        /// Gets a value indicating whether this token has fatal errors
        /// </summary>
        public bool HasErrors => _logger.HasErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlement"/> class
        /// </summary>
        public SoftwareEntitlement(ILogger logger)
        {
            var now = DateTimeOffset.Now;

            _logger = new MonitoringLogger(logger);
            _timestampParser = new TimestampParser(_logger);

            VirtualMachineId = string.Empty;
            Created = now;
            NotBefore = now;
            NotAfter = now + TimeSpan.FromDays(7);
        }

        /// <summary>
        /// Specify the virtual machine Id of the machine 
        /// </summary>
        /// <param name="virtualMachineId"></param>
        /// <returns></returns>
        public SoftwareEntitlement WithVirtualMachineId(string virtualMachineId)
        {
            if (string.IsNullOrWhiteSpace(virtualMachineId))
            {
                _logger.LogError("Virtual Machine ID must be specified");
                return this;
            }

            _logger.LogDebug("Virtual machine id is {VirtualMachineId}", virtualMachineId);

            return new SoftwareEntitlement(this, virtualMachineId: virtualMachineId);
        }

        /// <summary>
        /// Specify the time frame for which the token will be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <param name="notAfter">Latest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement ForTimeRange(string notBefore, string notAfter)
        {
            var start = NotBefore;
            var finish = NotAfter;

            if (!string.IsNullOrWhiteSpace(notBefore))
            {
                // TryParse will log any errors encountered, so we don't need explicit handling of the failure cases here
                var (successful, timestamp) = _timestampParser.TryParse(notBefore);
                if (successful)
                {
                    start = timestamp;
                }
            }

            if (!string.IsNullOrWhiteSpace(notAfter))
            {
                var (successful, timestamp) = _timestampParser.TryParse(notAfter);
                if (successful)
                {
                    finish = timestamp;
                }
            }

            return ForTimeRange(start, finish);
        }


        /// <summary>
        /// Specify the time frame for which the token will be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <param name="notAfter">Latest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement ForTimeRange(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            if (notAfter < notBefore)
            {
                _logger.LogError("Token expiry of {NotAfter} must be after token becomes active at {NotBefore}.", notAfter, notBefore);
                return this;
            }

            if (notAfter < DateTimeOffset.Now)
            {
                _logger.LogWarning("Token expiry of {NotAfter} has already passed", notAfter);
            }

            if (notBefore > DateTimeOffset.Now)
            {
                _logger.LogInformation("Token does not become available until {NotBefore}", notBefore);
            }

            return new SoftwareEntitlement(this, notBefore: notBefore, notAfter: notAfter);
        }

        /// <summary>
        /// Cloning constructor to initialize a new instance of the <see cref="SoftwareEntitlement"/> 
        /// class as a (near) copy of an existing one.
        /// </summary>
        /// <remarks>Specify any of the optional parameters to modify the clone from the original.</remarks>
        /// <param name="original">Original entitlement to clone.</param>
        /// <param name="notBefore">Optionally specify a new value for <see cref="NotBefore"/>.</param>
        /// <param name="notAfter">Optionally specify a new value for <see cref="NotAfter"/>.</param>
        /// <param name="virtualMachineId">Optionally specify a new value for <see cref="VirtualMachineId"/>.</param>
        public SoftwareEntitlement(
            SoftwareEntitlement original,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null,
            string virtualMachineId = null)
        {
            _logger = original._logger;
            _timestampParser = original._timestampParser;

            Created = original.Created;

            NotBefore = notBefore ?? original.NotBefore;
            NotAfter = notAfter ?? original.NotAfter;
            VirtualMachineId = virtualMachineId ?? original.VirtualMachineId;
        }
    }
}
