using Microsoft.AspNetCore.Mvc;

namespace SecureAuth.WebApp.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult PageNotFound()
        {
            return View();
        }
    }
}
