using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // для работы с Postgres
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling; // для чтения appsettings.json
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IConfiguration _configuration; // конфигурация приложения

        public AuthController(IConfiguration configuration) // внедрение конфигурации для получения доступа к строке подключения
        {
            _configuration = configuration;
        }

        [HttpPost("token")] // принимает POST-запросы /api/Auth/token
        public async Task<IActionResult> GetToken([FromBody] AuthRequest request)
        {
            bool isUserValid = await ValidateUser(request.Username, request.Password);

            if (isUserValid)
            {
                var tokenString = GenerateJwtToken(request.Username);
                return Ok(new { token = tokenString }); // возвращаем JSON с токеном
            }

            return Unauthorized("Invalid credentials.");
        }

        private async Task<bool> ValidateUser(string username, string password) // проверка пользователя по БД
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string storedHash = null;

            await using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)) // подключение к БД
            {
                await connection.OpenAsync();
                string sql = "SELECT \"PasswordHash\" FROM \"Users\" WHERE \"Username\" = @Username";
                await using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) // команда sql, которую хотим выполнить через подключение connection к БД
                {
                    cmd.Parameters.AddWithValue("Username", username); // имя пользователя добавляется в строку SQL как параметр (защита от SQL-инъекций)
                    object result = await cmd.ExecuteScalarAsync(); // выполнение команды и возвращение захэшированного пароля
                    if (result != null)
                        storedHash = result.ToString();
                }
            }

            return storedHash != null && BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        private string GenerateJwtToken(string username) // создание токена
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])); // получение секретного ключа из конфигурации
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);// создание учетных данных для подписи

            var claims = new[] // создание данных, лежащих внутри токена (полезная нагрузка / payload)
            {
                new Claim(JwtRegisteredClaimNames.Sub, username), // уникальное имя пользователя
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // уникальный ID токена
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120), // время жизни токена
                signingCredentials: credentials
                ); // создание самого токена

            return new JwtSecurityTokenHandler().WriteToken(token); // сериализуем токен в строку
        }
    }
}
