using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace SecureAuth.Api.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration; // конфигурация приложения

        public JwtService(IConfiguration configuration) // внедрение конфигурации для получения доступа к строке подключения
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(string username) // создание токена
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])); // получение секретного ключа из конфигурации
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);// создание учетных данных для подписи

            var claims = new[] // создание данных, лежащих внутри токена (полезная нагрузка / payload)
            {
                new Claim(JwtRegisteredClaimNames.Sub, username), // уникальное имя пользователя
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // уникальный ID токена
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120), // время жизни токена
                signingCredentials: credentials
                ); // создание самого токена

            return new JwtSecurityTokenHandler().WriteToken(token); // сериализуем токен в строку
        }
    }
}
