using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql; // для работы с Postgres
using SecureAuth.WebApp.Models;
using SecureAuth.WebApp.Services; // для работы с JwtService
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SecureAuth.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration; // конфигурация приложения
        private readonly JwtService _jwtService; // сервис для работы с JWT

        public AccountController(IConfiguration configuration, JwtService jwtService) // внедрение конфигурации для получения доступа к строке подключения и настройка JWT
        {
            _configuration = configuration;
            _jwtService = jwtService;
        }


        // метод вызывается, когда пользователь заходит на /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // метод вызывается, когда пользователь нажмет "Войти" (Атрибут type="submit" при нажатии кнопки заставляет браузер найти родительскую форму <form>, содержащую атрибут method="post", и отправить HTTP POST-запрос)
        [HttpPost]
        [ValidateAntiForgeryToken] // отклонение подделанных POST-запросов с других сайтов
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {   
            if (await ValidateUser(username, password))
            {
                // Создаем профиль пользователя для Cookie
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, username)};
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties();

                // Выполнение входа через Cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties); // Создание зашифрованной HttpOnly Cookie

                // Генерация JWT для API-клиентов
                var token = _jwtService.GenerateJwtToken(username);

                // Добавление JWT в обычную Cookie
                Response.Cookies.Append("JwtToken", token, new CookieOptions
                {
                    HttpOnly = true, // Cookie недоступна для JavaScript на клиенте (защита от XSS-атак)
                    Secure = true, // Cookie будет передаваться только по HTTPS-соединению (защита от MITM-атак)
                    SameSite = SameSiteMode.Strict, // Cookie не будет отправляться вместе с кросс-сайтовыми запросами (защита от CSRF-атак)
                    Expires = DateTime.UtcNow.AddMinutes(120) // установка времени жизни Cookie в 120 минут
                });

                return RedirectToAction("Index", "Home"); // перенаправление на главную страницу после успешного входа
            }
            ModelState.AddModelError(string.Empty, "Invalid username or password."); // если пользователь не прошел валидацию, добавляем ошибку в ModelState
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Выполнение выхода из системы 
            Response.Cookies.Delete("JwtToken"); // Удаление JWT Cookie
            return RedirectToAction("Login", "Account"); // перенаправление на страницу входа после выхода
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


        // метод вызывается, когда пользователь заходит на /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // метод вызывается, когда пользователь нажмет "Зарегестрироватсья" (Атрибут type="submit" при нажатии кнопки заставляет браузер найти родительскую форму <form>, содержащую атрибут method="post", и отправить HTTP POST-запрос
        [HttpPost]
        [ValidateAntiForgeryToken] // отклонение подделанных POST-запросов с других сайтов
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model)
        {
            if (ModelState.IsValid) // если данные прошли валидацию (атрибуты в RegisterViewModel)
            {
                if (await IsUsernameTaken(model.Username)) // имя пользователя занято
                {
                    ModelState.AddModelError(nameof(model.Username), "This username is already occupied.");
                    return View(model);
                }
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password); // хэширование пароля с помощью BCrypt
                await CreateUser(model.Username, passwordHash); // сохраняем нового пользователя в БД
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        private async Task<bool> IsUsernameTaken(string username)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            await using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)) // подключение к БД
            {
                await connection.OpenAsync();
                string sql = "SELECT COUNT(1) FROM \"Users\" WHERE \"Username\" = @Username";
                await using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) // команда sql, которую хотим выполнить через подключение connection к БД
                {
                    cmd.Parameters.AddWithValue("Username", username); // имя пользователя добавляется в строку SQL как параметр (защита от SQL-инъекций)
                    object result = await cmd.ExecuteScalarAsync(); // выполнение команды 
                    return (long)result! > 0; // если результат больше 0, значит имя занято
                }
            }
        }

        private async Task CreateUser(string username, string passwordHash)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            await using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)) // подключение к БД
            {
                await connection.OpenAsync();
                string sql = "INSERT INTO \"Users\" (\"Username\", \"PasswordHash\") VALUES (@Username, @PasswordHash)";
                await using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) // команда sql, которую хотим выполнить через подключение connection к БД
                {
                    cmd.Parameters.AddWithValue("Username", username); // имя пользователя добавляется в строку SQL как параметр (защита от SQL-инъекций)
                    cmd.Parameters.AddWithValue("PasswordHash", passwordHash); // хэш пароля добавляется в строку SQL как параметр (защита от SQL-инъекций)
                    await cmd.ExecuteNonQueryAsync(); // выполнение команды
                }
            }
        }
    };
}
