namespace HashGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("BCrypt Hash Generator");
            Console.WriteLine("---------------------");

            // Бесконечный цикл, чтобы можно было генерировать много хэшей без перезапуска
            while (true)
            {
                Console.Write("\nEnter password to hash (or type 'exit' to close): ");

                // Добавляем '?' чтобы исправить предупреждение (warning)
                string? passwordToHash = Console.ReadLine();

                if (string.IsNullOrEmpty(passwordToHash))
                {
                    Console.WriteLine("Password cannot be empty.");
                    continue;
                }

                if (passwordToHash.ToLower() == "exit")
                {
                    break;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nGenerated Hash:");
                Console.ResetColor();
                Console.WriteLine(hashedPassword);
                Console.WriteLine("---------------------");
            }
        }
    }
}