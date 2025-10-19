using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


namespace SecureAuth.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Настройка сервисов 

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // добавление и настройка сервисов аутентификации
            builder.Services.AddAuthentication(options =>
            {
                // Установка схемы аутентификации
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => 
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

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection(); // перенаправление всех HTTP-запросов на HTTPS
            app.UseRouting(); // выбор эндпоинта (метода контроллера) для обработки запроса
            app.UseAuthentication(); // аутентификация 
            app.UseAuthorization(); // авторизация
            app.MapControllers(); // использование маршрутизации для всех контроллеров

            app.Run(); // запуск приложения
        }
    }
}
