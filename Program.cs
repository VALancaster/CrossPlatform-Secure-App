using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// 1. Настройка сервисов 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(); // Добавление в приложение сервисов, необходимых для работы по шаблону MVC


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer( options => // добавление и настройка сервисов аутентификации
    {
        // Настраиваем правила проверки входящих Jwt-токенов
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // проверять издателя токена
            ValidateAudience = true, // проверять потребителя токена
            ValidateLifetime = true, // проверять срок действия токена
            ValidateIssuerSigningKey = true, // проверять подпись токена
            // Валидные значения для проверки:
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // ожидаемый издатель (берется из secrets.json)
            ValidAudience = builder.Configuration["Jwt:Audience"], // ожидаемый потребитель (берется из secrets.json)
            // Создание ключа для проверки записи
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // Берем секретный ключ из secrets.json и преобразуем в байты
        };
    });


// 2. Настройка конвейера обработки запросов

var app = builder.Build(); // сборка настроенных сервисов в готовое приложение 

app.UseStatusCodePagesWithReExecute("/Error/{0}"); // настройка обработки кодов состояния (404)

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

app.MapControllers(); // использование маршрутизации для всех контроллеров
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); // настройка марщрутизации по уиолчанию для MVC

// --- НАЧАЛО ИСПРАВЛЕННОГО ДИАГНОСТИЧЕСКОГО БЛОКА ---
app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) => // https://localhost:7105/debug/routes
{
    var endpoints = endpointSources.SelectMany(es => es.Endpoints);
    var output = endpoints
        .OfType<RouteEndpoint>() // Выбираем только эндпоинты, у которых есть маршрут
        .Select(e =>
        {
            var controller = e.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerName;
            var action = e.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ActionName;
            var pattern = e.RoutePattern.RawText; // <-- ИЗМЕНЕНИЕ ЗДЕСЬ
            var httpMethods = string.Join(", ", e.Metadata.OfType<HttpMethodMetadata>().SelectMany(m => m.HttpMethods));

            return $"Pattern: {pattern} | Methods: {httpMethods} | Controller: {controller}.{action}";
        });
    return string.Join("\n", output);
});
// --- КОНЕЦ ИСПРАВЛЕННОГО ДИАГНОСТИЧЕСКОГО БЛОКА ---

app.Run(); // запуск приложения