using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using SecureAuth.Api.Services;


namespace SecureAuth.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. ��������� �������� 

            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(listenOptions =>
                {
                    listenOptions.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        "device.pfx",
                        builder.Configuration["Kestrel:Certificates:Default:Password"]
                    );
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddGrpc(); // ���������� �������� ��� gRPC

            builder.Services.AddSingleton<JwtService>(); // ����������� ������� ��� ������ � JWT-��������
            builder.Services.AddScoped<UserService>(); // ����������� ������� ��� ������ � �������������� � ��

            // ���������� � ��������� �������� ��������������
            builder.Services.AddAuthentication(options =>
            {
                // ��������� ����� ��������������
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // ����������� ������� �������� �������� Jwt-�������
                options.TokenValidationParameters = new TokenValidationParameters
                {
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

            // 2. ��������� ��������� ��������� ��������

            var app = builder.Build(); // ������ ����������� �������� � ������� ���������� 

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection(); // ��������������� ���� HTTP-�������� �� HTTPS
            app.UseRouting(); // ����� ��������� (������ �����������) ��� ��������� �������
            app.UseAuthentication(); // �������������� 
            app.UseAuthorization(); // �����������
            app.MapControllers(); // ������������� ������������� ��� ���� ������������
            app.MapGrpcService<Services.AuthGrpcService>(); // ����������� gRPC-�������

            app.Urls.Add("http://*:8080"); // прослушивание всех сетевых интерфейсов внутри контейнера

            app.Run(); // ������ ����������
        }
    }
}
