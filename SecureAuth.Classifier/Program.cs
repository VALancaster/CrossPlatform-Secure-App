using SecureAuth.Classifier.Services;

namespace SecureAuth.Classifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Регистрация инфраструктуры gRPC
            builder.Services.AddGrpc();

            var app = builder.Build();

            // Регистрация сервиса в сетевой карте веб-сервера
            app.MapGrpcService<EcgClassifierService>();

            app.Urls.Add("http://*:8080");

            app.Run();
        }
    }
}