using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // для работы с Postgres
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.Marshalling; // для чтения appsettings.json
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SecureAuthPrototype.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration; // конфигурация приложения

        public AccountController(IConfiguration configuration) // внедрение конфигурации для получения доступа к строке подключения
        {
            _configuration = configuration;
        }

        
        // метод вызывается, когда пользователь заходит на /Account/Login
        [HttpGet("Login")] // ответ на GET-запрос
        public IActionResult Login()
        {
            return View();
        }


        // метод вызывается, когда пользователь нажмет "Войти" (Атрибут type="submit" при нажатии кнопки заставляет браузер найти родительскую форму <form>, содержащую атрибут method="post", и отправить HTTP POST-запрос)
        [HttpPost("Login")] // принимает POST-запросы
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            bool isUserValid = await ValidateUser(username, password);

            if (isUserValid)
            {
                // Успешный вход через браузер
                // TODO: создание Cookie
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["ErrorMessage"] = "Invalid username or password.";
                return View();
            }
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
    }
}
