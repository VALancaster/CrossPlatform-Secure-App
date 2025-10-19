using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace SecureAuth.Api.Controllers
{
    [ApiController]
    [Route("api/SecretData")] // Базовый маршрут будет /api/SecretData
    [Authorize]
    public class SecretDataController : ControllerBase
    {
        [HttpGet] // Отвечает на HTTPGET-запрос /api/SecretData
        public IActionResult GetSecretData()
        {
            // Получаем имя пользователя из "утверждений" (claims) в токене
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;

            var data = new
            {
                Message = $"Hello, {username}! This is a top secret message from the API.",
                Timestamp = DateTime.UtcNow
            };

            return Ok(data);
        }

        [HttpGet("admin-only")] // Отвечает на HTTPGET-запросы /api/SecretData/admin-only\
        [Authorize(Roles="Admin")] //  Доступ только для пользователей с ролью "Admin"
        public IActionResult GetAdminLevelSecretData()
        {
            var username = User.Identity?.Name;

            return Ok(new { Message = $"Welcome, Administrator {username}! This is data for admins only."});
        }
    }
}
