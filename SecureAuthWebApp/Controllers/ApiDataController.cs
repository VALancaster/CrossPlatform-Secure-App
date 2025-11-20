using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecureAuth.WebApp.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class ApiDataController : ControllerBase
    {
        [HttpGet("secret")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetSecretData()
        {
            return Ok(new
            {
                Name = User.Identity?.Name,
                Message = "This is protected data accessible only with a valid JWT token."
            });
        }
    }
}
