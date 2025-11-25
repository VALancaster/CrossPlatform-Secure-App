using Grpc.Core;
using SecureAuth.Grpc; // пространство имен из auth.proto

namespace SecureAuth.Api.Services
{
    public class AuthGrpcService : Authenticator.AuthenticatorBase // наследник автосгенерированного базового класса
    {
        private readonly ILogger<AuthGrpcService> _logger;
        private readonly UserService _userService;
        private readonly JwtService _jwtService;

        public AuthGrpcService(ILogger<AuthGrpcService> logger, UserService userService, JwtService jwtService)
        {
            _logger = logger;
            _userService = userService;
            _jwtService = jwtService;
        }

        public override async Task<AuthReply> GetToken(AuthRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"gRPC request for user: {request.Username}");

            bool isUserValid = await _userService.ValidateUser(request.Username, request.Password);

            if (isUserValid)
            {
                _logger.LogInformation($"User '{request.Username}' authenticated successfully via gRPC.");

                var token = _jwtService.GenerateJwtToken(request.Username);

                return new AuthReply
                {
                    Success = true,
                    JwtToken = token
                };

            }

            _logger.LogInformation($"gRPC authentication failed for user: {request.Username}");

            return new AuthReply
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }

    }
}