using Npgsql; // для работы с Postgres


namespace SecureAuth.Api.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration; // конфигурация приложения

        public UserService(IConfiguration configuration) // внедрение конфигурации для получения доступа к строке подключения
        {
            _configuration = configuration;
        }

        public async Task<bool> ValidateUser(string username, string password) // проверка пользователя по БД
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
    }
}
