using Microsoft.AspNetCore.Mvc;

namespace SecureAuthPrototype.Controllers
{
    public class AccountController : Controller
    {

        // метод вызывается, когда пользователь заходит на /Account/Login
        [HttpGet] // ответ на GET-запрос
        public IActionResult Login()
        {
            return View();
        }

    }
}
