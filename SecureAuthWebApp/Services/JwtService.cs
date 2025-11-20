using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SecureAuth.WebApp.Services
{
    public class JwtService
    {
        // Приватные поля для хранения настроек, полученных при старте приложения
        private readonly string _issuer; // издатель токена
        private readonly string _audience; // потребитель токена
        private readonly SymmetricSecurityKey _securityKey; // секретный ключ для подписи токена

        public JwtService(IConfiguration configuration) // конструктор сервиса, получающий настройки из Program.cs
        {
            _issuer = configuration["Jwt:Issuer"]!;
            _audience = configuration["Jwt:Audience"];
            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)); // получение секретного ключа из конфигурации
        }

        public string GenerateJwtToken(string username) // создание токена
        {
            var claims = new[] // создание данных, лежащих внутри токена (полезная нагрузка / payload)
            {
                new Claim(JwtRegisteredClaimNames.Sub, username), // уникальное имя пользователя
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // уникальный ID токена
            };

            var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);// создание учетных данных для подписи

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(120), // время жизни токена
                signingCredentials: credentials
                ); // создание объекта токена

            return new JwtSecurityTokenHandler().WriteToken(token); // сериализуем токен в строку
        }

    }
}
