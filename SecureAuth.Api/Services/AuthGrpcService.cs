using Grpc.Core;
using SecureAuth.Grpc; // пространство имен из auth.proto

namespace SecureAuth.Api.Services
{
    public class AuthGrpcService : Authenticator.AuthenticatorBase // наследник автосгенерированного базового класса
    {
        private readonly ILogger<AuthGrpcService> _logger;

        public AuthGrpcService(ILogger<AuthGrpcService> logger)
        {
            _logger = logger;
        }

        public override async Task<AuthReply> GetToken(AuthRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"gRPC request for user: {request.Username}");

            if (request.Username == "admin" && request.Password == "admin")
            {
                return new AuthReply
                {
                    Success = true,
                    JwtToken = "заглушка"
                };

            }

            return new AuthReply
            {
                Success = false,
                ErrorMessage = "Invalid credentials"
            };
        }

    }
}