using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kestrelssl.Controller
{
    //[Authorize]
    [Route("api/v1/hubs")]
    [Consumes("application/json")]
    public class HubProxy : Microsoft.AspNetCore.Mvc.ControllerBase //Microsoft.AspNetCore.Mvc.Controller
    {
        // POST /api/v1/hubs/chat/users/1
        [HttpPost("{hub}/users/{id}")]
        public IActionResult SendToUser(string hub, string id, [FromBody] PayloadMessage message)
        {
            return Accepted();
        }
    }
}
