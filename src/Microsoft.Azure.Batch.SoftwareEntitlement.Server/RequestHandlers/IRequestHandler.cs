using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.RequestHandlers
{
    public interface IRequestHandler<in TRequestBody>
    {
        Response Handle(HttpContext httpContext, TRequestBody requestBody);
    }
}
