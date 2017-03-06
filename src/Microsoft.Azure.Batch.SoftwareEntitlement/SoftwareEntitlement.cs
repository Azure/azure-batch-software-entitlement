using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;

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
        public string VirtualMachineId { get; private set; }

        /// <summary>
        /// The moment at which the entitlement was created
        /// </summary>
        public DateTimeOffset Created { get; private set; }

        /// <summary>
        /// The earliest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotBefore { get; private set; }

        /// <summary>
        /// The latest moment at which the entitlement is active
        /// </summary>
        public DateTimeOffset NotAfter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this token has fatal errors
        /// </summary>
        public bool HasErrors => _logger.HaveLoggedErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlement"/> class
        /// </summary>
        public SoftwareEntitlement(ISimpleLogger logger)
        {
            var now = DateTimeOffset.Now;

            _logger = new MonitoringLogger(logger);
            _timestampParser = new TimestampParser(_logger);

            VirtualMachineId = string.Empty;
            Created = now;
            NotBefore = now;
            NotAfter = now + TimeSpan.FromDays(7);

            _logger.Debug("Token creation {Timestamp:d/mmm/yyyy HH:MM}", now);
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
                _logger.Error("Virtual Machine ID must be specified");
                return this;
            }

            _logger.Debug("Virtual machine id is {VirtualMachineId}", virtualMachineId);

            return new SoftwareEntitlement(this)
            {
                VirtualMachineId = virtualMachineId
            };
        }

        /// <summary>
        /// Specify the time frame for which the token will be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <param name="notAfter">Latest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement ForTimespan(string notBefore, string notAfter)
        {
            if (!_timestampParser.TryParse(notBefore, out var start))
            {
                start = NotBefore;
                _logger.Warning("No valid token start specified; using {NotBefore}", start);
            }

            if (!_timestampParser.TryParse(notAfter, out var finish))
            {
                finish = NotAfter;
                _logger.Warning("No valid token expiry specified; using {NotAfter}", finish);
            }

            return ForTimespan(start, finish);
        }


        /// <summary>
        /// Specify the time frame for which the token will be valid
        /// </summary>
        /// <param name="notBefore">Earliest instant of availability.</param>
        /// <param name="notAfter">Latest instant of availability.</param>
        /// <returns></returns>
        public SoftwareEntitlement ForTimespan(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            if (notAfter < notBefore)
            {
                _logger.Error("Token expiry of {NotAfter} must be after token becomes active at {NotBefore}.", notAfter, notBefore);
                return this;
            }

            if (notAfter < DateTimeOffset.Now)
            {
                _logger.Warning("Token expiry of {NotAfter} has already passed", notAfter);
            }

            if (notBefore > DateTimeOffset.Now)
            {
                _logger.Information("Token does not become available until {NotBefore}", notBefore);
            }

            var result = new SoftwareEntitlement(this)
            {
                NotBefore = notBefore,
                NotAfter = notAfter
            };

            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareEntitlement"/> class
        /// </summary>
        /// <param name="original">Original entitlement to clone.</param>
        public SoftwareEntitlement(SoftwareEntitlement original)
        {
            _logger = original._logger;
            _timestampParser = original._timestampParser;

            VirtualMachineId = original.VirtualMachineId;
            Created = original.Created;
            NotBefore = original.NotBefore;
            NotAfter = original.NotAfter;
        }
    }
}
