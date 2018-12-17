using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RouteConstraints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MissingVersionConstraintAttribute : Attribute, IActionConstraint
    {
        public int Order { get; set; }

        public bool Accept(ActionConstraintContext context)
        {
            var queryParameters = context.RouteContext.HttpContext.Request.Query;
            if (queryParameters.ContainsKey(ApiVersions.ParameterName))
            {
                return false;
            }

            return true;
        }
    }
}
