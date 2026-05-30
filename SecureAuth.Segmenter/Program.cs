using SecureAuth.Segmenter.Services;

namespace SecureAuth.Segmenter
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
            app.MapGrpcService<EcgSegmenterService>();

            app.Urls.Add("http://*:8080");

            app.Run();
        }
    }
}