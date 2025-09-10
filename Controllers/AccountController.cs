using Microsoft.AspNetCore.Mvc;
using Npgsql; // для работы с Postgres
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices.Marshalling; // для чтения appsettings.json

namespace SecureAuthPrototype.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration; // конфигурация приложения

        public AccountController(IConfiguration configuration) // внедрение конфигурации для получения доступа к строке подключения
        {
            _configuration = configuration;
        }

        // метод вызывается, когда пользователь заходит на /Account/Login
        [HttpGet] // ответ на GET-запрос
        public IActionResult Login()
        {
            return View();
        }

        // метод вызывается, когда пользователь нажмет "Войти" (Атрибут type="submit" при нажатии кнопки заставляет браузер найти родительскую форму <form>, содержащую атрибут method="post", и отправить HTTP POST-запрос)
        [HttpPost] // принимает POST-запросы
        public async Task<IActionResult> Login(string username, string password)
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

            if (storedHash != null)
            {
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
                if (isPasswordValid)
                    return Content("Login successful!");
            }

            return Content("Invalid username or password.");
        }
    }
}
