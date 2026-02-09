using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using SecureAuth.Api.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace SecureAuth.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. ��������� �������� 

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddGrpc(); // ���������� �������� ��� gRPC

            builder.Services.AddSingleton<JwtService>(); // ����������� ������� ��� ������ � JWT-��������
            builder.Services.AddScoped<UserService>(); // ����������� ������� ��� ������ � �������������� � ��

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis_Host"] + ":6379";
                options.InstanceName = "RateLimitInstance";
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // глобальная настройка 

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetTokenBucketLimiter(partitionKey, key =>
                        new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10, // максимальное количество запросов сразу
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst, // порядок обработки запросов в очереди
                            QueueLimit = 0, // максимальное количество запросов в очереди
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1), // период пополнения токенов
                            TokensPerPeriod = 2, // добавлять 2 токена в секунду
                            AutoReplenishment = true // автоматическое пополнение
                        });
                });
            });

            // ���������� � ��������� �������� ��������������
            builder.Services.AddAuthentication(options =>
            {
                // ��������� ����� ��������������
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                // ����������� ������� �������� �������� Jwt-�������
                    ValidateIssuer = true, // ��������� �������� ������
                    ValidateAudience = true, // ��������� ����������� ������
                    ValidateLifetime = true, // ��������� ���� �������� ������
                    ValidateIssuerSigningKey = true, // ��������� ������� ������
                    // �������� �������� ��� ��������:
                    ValidIssuer = builder.Configuration["Jwt:Issuer"], // ��������� �������� (������� �� secrets.json)
                    ValidAudience = builder.Configuration["Jwt:Audience"], // ��������� ����������� (������� �� secrets.json)
                    // �������� ����� ��� �������� ������
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // ����� ��������� ���� �� secrets.json � ����������� � �����
                };
            });

            builder.Services.AddRateLimiter(options =>
            {
                // политика ограничения "fixed"
                options.AddFixedWindowLimiter(policyName: "fixed", fixedOptions =>
                {
                    fixedOptions.PermitLimit = 10; // Максимальное разрешенное количество запросов
                    fixedOptions.Window = TimeSpan.FromSeconds(10); // За 10 секунд
                    fixedOptions.QueueLimit = 0; // Не ставить в очередь 
                });

                // политика по умолчанию: если [EnableRateLimiting] не указан, использовать эту
                options.RejectionStatusCode = 429; // 429 Too Many Requests
            });

            // Регистрация Redis (может понадобиться при масштабировании)
            // builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = "redis:6379"; } );

            // 2. ��������� ��������� ��������� ��������

            var app = builder.Build(); // ������ ����������� �������� � ������� ���������� 

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting(); // ����� ��������� (������ �����������) ��� ��������� �������
            app.UseRateLimiter();
            app.UseAuthentication(); // �������������� 
            app.UseAuthorization(); // �����������
            app.MapControllers(); // ������������� ������������� ��� ���� ������������
            app.MapGrpcService<Services.AuthGrpcService>(); // ����������� gRPC-�������

            app.Urls.Add("http://*:8080"); // прослушивание всех сетевых интерфейсов внутри контейнера

            app.Run(); // ������ ����������
        }
    }
}
