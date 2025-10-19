using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


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

            app.Run(); // ������ ����������
        }
    }
}
