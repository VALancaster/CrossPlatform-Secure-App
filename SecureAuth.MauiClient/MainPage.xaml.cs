using Grpc.Net.Client;
using SecureAuth.Grpc; // пространство имен из auth.proto
using Microsoft.Extensions.DependencyInjection;

namespace SecureAuth.MauiClient
{
    public partial class MainPage : ContentPage
    {
        private Authenticator.AuthenticatorClient? _grpcClient; // поле для хранения gRPC клиента

        public MainPage(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            InitializeGrpcClient(serviceProvider); // инициализация gRPC клиента при создании страницы
        }

        private void InitializeGrpcClient(IServiceProvider serviceProvider)
        {
            var serverAddress = "http://80.90.187.200:8001";

#if WINDOWS
            // на Windows используется специальный HttpMessageHandler, зарегестрированный в MauiProgram.cs
            var httpHandler = serviceProvider.GetService<HttpMessageHandler>();
            var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions { HttpHandler = httpHandler });
#else
            // на других платформах используется стандартный обработчик
            var channel = GrpcChannel.ForAddress(serverAddress);
#endif

            _grpcClient = new Authenticator.AuthenticatorClient(channel);
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            if (_grpcClient == null)
            {
                txtResult.Text = "gRPC client is not initialized.";
                return;
            }

            // блокировка кнопки, пока идет запрос
            LoginButton.IsEnabled = false;
            txtResult.Text = "Sending gRPC request...";

            try
            {
                // сбор данных из полей ввода
                var request = new AuthRequest
                {
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                };

                // асинхронный вызов метода на сервере
                var reply = await _grpcClient.GetTokenAsync(request);

                // показ результата
                if (reply.Success)
                {
                    txtResult.Text = $"SUCCESS!\n\nToken:\n{reply.JwtToken}";
                }
                else
                {
                    txtResult.Text = $"ERROR:\n{reply.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = $"EXCEPTION:\n{ex.Message}"; // показ ошибки, если сервер недоступен или другая проблема
            }
            finally
            {
                LoginButton.IsEnabled = true; // включение кнопки обратно
            }
        }
    }
}
