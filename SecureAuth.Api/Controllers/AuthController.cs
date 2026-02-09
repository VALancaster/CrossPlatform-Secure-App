using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling; // для чтения appsettings.json
using System.Threading.Tasks;
using SecureAuth.Api.Services;
using Microsoft.AspNetCore.RateLimiting;


namespace SecureAuthPrototype.Controllers
{
    public class AuthRequest // модель для приема JSON
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


    [ApiController]
    [Route("api/[controller]")] // базовый маршрут будет /api/Auth
    public class AuthController : ControllerBase // API-контроллеры обычно наследуются от ControllerBase
    {
        private readonly UserService _userService; // сервис для проверки пользователя
        private readonly JwtService _jwtService; // сервис для создания JWT-токенов


        public AuthController(UserService userService, JwtService jwtService) // внедрение сервисов
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("token")] // принимает POST-запросы /api/Auth/token
        [EnableRateLimiting("fixed")] // применение политики ограничения запросов "fixed"
        public async Task<IActionResult> GetToken([FromBody] AuthRequest request)
        {
            bool isUserValid = await _userService.ValidateUser(request.Username, request.Password);

            if (isUserValid)
            {
                var tokenString = _jwtService.GenerateJwtToken(request.Username);
                return Ok(new { token = tokenString }); // возвращаем JSON с токеном
            }

            return Unauthorized("Invalid credentials.");
        }
    }
}
