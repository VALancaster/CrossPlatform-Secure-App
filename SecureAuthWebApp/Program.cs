using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureAuth.WebApp.Services;
using System.Text;


namespace SecureAuth.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Настройка сервисов 

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(); // Добавление в приложение сервисов, необходимых для работы по шаблону MVC

            builder.Services.AddSingleton<JwtService>(); // Регистрация сервиса для работы с JWT в DI-контейнере как Singleton (один экземпляр на все приложение)

            // Добавление и настройка сервисов аутентификации на основе Cookie
            builder.Services.AddAuthentication(options => // Устанаваливаем Cookie как схему по умолчанию ([Authorize] без параметров будет использовать Cookie)
            { 
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }) 
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => // Регистрируем обработчик для Cookie-аутентификации
            {
                options.LoginPath = "/Account/Login"; // перенаправление, если пользователь не авторизован
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // Регистрируем обработчик для JWT
            {
                /*
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context => // Событие при получении запроса
                    {
                        context.Token = context.Request.Cookies["AuthCookie"]; // Попытка чтения Cookie с именем "AuthCookie"
                        return Task.CompletedTask;
                    }
                };
                */

                options.TokenValidationParameters = new TokenValidationParameters // Настраиваем правила проверки входящих Jwt-токенов
                {
                    ValidateIssuer = true, // проверять издателя токена
                    ValidateAudience = true, // проверять потребителя токена
                    ValidateLifetime = true, // проверять срок действия токена
                    ValidateIssuerSigningKey = true, // проверять подпись токена
                    // Валидные значения для проверки:
                    ValidIssuer = builder.Configuration["Jwt:Issuer"], // ожидаемый издатель (берется из secrets.json)
                    ValidAudience = builder.Configuration["Jwt:Audience"], // ожидаемый потребитель (берется из secrets.json)
                    // Создание ключа для проверки записи
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // Берем секретный ключ из secrets.json и преобразуем в байты
                };
            });

            builder.Services.AddAuthorization(); // Регистрируем сервис авторизации

            /*
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login"; // перенаправление неаутентифицированных пользователей при попытке получения доступа к защищенному ресурсу
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // установка времени жизни Cookie в 30 минут
                options.SlidingExpiration = true; // продление времени жизни Cookie при активности пользователя
            });
            */


            // 2. Настройка конвейера обработки запросов

            var app = builder.Build(); // сборка настроенных сервисов в готовое приложение 

            app.UseStatusCodePagesWithReExecute("/Error/{0}"); // настройка обработки кодов состояния (404 и другие)

            if (!app.Environment.IsDevelopment()) // выполнится только в Production-режиме (не при разработке)
            {
                app.UseExceptionHandler("/Home/Error"); // настройка глобального обработчика исключений
                app.UseHsts(); // включение  HSTS, заставляющего браузер общаться с сайтом только по HTTPS
            }

            app.UseHttpsRedirection(); // перенаправление всех HTTP-запросов на HTTPS
            app.UseStaticFiles(); // возможность серверу отдавать статические файлы (CSS, JS) из папки wwwroot
            app.UseRouting(); // выбор эндпоинта (метода контроллера) для обработки запроса
            app.UseAuthentication(); // аутентификация 
            app.UseAuthorization(); // авторизация

            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); // настройка марщрутизации по умолчанию для MVC

            app.Run(); // запуск приложения
        }
    }
}
