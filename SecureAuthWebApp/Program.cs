using Microsoft.AspNetCore.Authentication.Cookies;


namespace SecureAuth.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Настройка сервисов 

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(); // Добавление в приложение сервисов, необходимых для работы по шаблону MVC

            // Добавление и настройка сервисов аутентификации на основе Cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login"; // перенаправление неаутентифицированных пользователей при попытке получения доступа к защищенному ресурсу
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // установка времени жизни Cookie в 30 минут
                options.SlidingExpiration = true; // продление времени жизни Cookie при активности пользователя
            });

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
