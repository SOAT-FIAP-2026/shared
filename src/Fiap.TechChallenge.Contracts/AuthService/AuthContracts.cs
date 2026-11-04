namespace Fiap.TechChallenge.Contracts.AuthService;

/// <summary>
/// Constantes padronizadas de autenticação JWT e roles compartilhadas entre:
/// <c>lambda-auth-function</c>, <c>fase1-tech-challenge</c>, <c>os-microservice</c>,
/// <c>execution-service</c> e <c>billing-service</c>.
/// </summary>
public static class JwtAuthConstants
{
    public const string DefaultIssuer = "TechChallenge";
    public const string DefaultAudience = "techchallenge.com.br";
    public const string SsmJwtSecretParameter = "/techchallenge/prod/jwt_secret";
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public static class Claims
    {
        public const string UsuarioId = "UsuarioId";
        public const string Cpf = "cpf";
    }

    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Operador = "Operador";
        public const string Mecanico = "Mecanico";
    }
}

/// <summary>Request enviado para <c>POST /auth/token</c> na <c>lambda-auth-function</c>.</summary>
public sealed record AuthTokenRequest(string Cpf);

/// <summary>Response retornado por <c>POST /auth/token</c> na <c>lambda-auth-function</c>.</summary>
public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType = "Bearer",
    int ExpiresIn = 3600);

/// <summary>Request de autenticação enviado para <c>POST /api/v1/autenticacao/login</c> no <c>fase1-tech-challenge</c>.</summary>
public sealed record StaffLoginRequest(string Login, string Senha);

/// <summary>Response retornado por <c>POST /api/v1/autenticacao/login</c> no <c>fase1-tech-challenge</c>.</summary>
public sealed record StaffLoginResponse(string Token, string NomeUsuario);

/// <summary>Request de cadastro enviado para <c>POST /api/v1/autenticacao/cadastrar</c> no <c>fase1-tech-challenge</c>.</summary>
public sealed record StaffCadastrarRequest(
    string Nome,
    string Email,
    string Login,
    string Senha,
    Guid IdPermissao);
