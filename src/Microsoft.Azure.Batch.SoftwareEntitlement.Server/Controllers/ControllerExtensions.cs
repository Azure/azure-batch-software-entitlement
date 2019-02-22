using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Model;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers
{
    public static class ControllerExtensions
    {
        public static IActionResult CreateActionResult(
            this Controller controller,
            Response response)
        {
            if (response.Value == null)
            {
                return controller.StatusCode(response.StatusCode);
            }

            return controller.StatusCode(response.StatusCode, response.Value);
        }
    }
}
