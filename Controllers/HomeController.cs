using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SecureAuthPrototype.Models;
using Microsoft.AspNetCore.Authorization;

namespace SecureAuthPrototype.Controllers
{

    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize] // доступ к этому методу только у пользователей с валидным JWT-токеном
        [HttpGet("GetSecretData")] // метод отвечает на HTTPGET-запросы
        public IActionResult GetSecretData()
        {
            var data = new { Message = "This is a secret message only for aythorized users!" };
            return Ok(data); // возврат ответа 200 OK с данными в теле
        }
    }
}
