using System.Text.Json.Serialization;

namespace Fiap.TechChallenge.Contracts.AuthService;

/// <summary>
/// Constantes de autenticação JWT e roles usadas por <c>fase1-tech-challenge</c>, <c>os-microservice</c>,
/// <c>execution-service</c> e <c>lambda-auth-function</c>. O <c>billing-service</c> não valida JWT.
/// </summary>
public static class JwtAuthConstants
{
    /// <summary>Issuer validado por <c>os-microservice</c> e <c>execution-service</c>.</summary>
    /// <remarks>
    /// Diverge de <see cref="LambdaAuth.IssuerPadrao"/> (também configurado em
    /// <c>soat-infra/environments/dev/main.tf</c>): o token do lambda-auth falha a validação de issuer nesses serviços.
    /// </remarks>
    public const string DefaultIssuer = "TechChallenge";

    /// <summary>Audience validado por <c>os-microservice</c> e <c>execution-service</c>.</summary>
    /// <remarks>Diverge de <see cref="LambdaAuth.AudiencePadrao"/>.</remarks>
    public const string DefaultAudience = "techchallenge.com.br";

    public const string SsmJwtSecretParameter = "/techchallenge/prod/jwt_secret";
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>Nomes de claims.</summary>
    /// <remarks>
    /// O fase1 emite <see cref="UsuarioId"/>, <c>ClaimTypes.Name</c>, <c>ClaimTypes.Email</c> e <c>ClaimTypes.Role</c>.
    /// O lambda-auth emite só <see cref="Sub"/> (id do cliente), <see cref="Jti"/> e <see cref="Cpf"/>, sem role e sem
    /// <see cref="UsuarioId"/>. O execution-service lê o usuário em <see cref="UsuarioId"/>, depois
    /// <c>NameIdentifier</c>, depois <see cref="Sub"/>.
    /// </remarks>
    public static class Claims
    {
        public const string UsuarioId = "UsuarioId";
        public const string Cpf = "cpf";
        public const string Sub = "sub";
        public const string Jti = "jti";
    }

    /// <summary>Valores reais da <c>lambda-auth-function</c> (<c>POST /auth/token</c>, HS256).</summary>
    public static class LambdaAuth
    {
        public const string Rota = "auth/token";
        public const string IssuerPadrao = "fiap-tech-challenge";
        public const string AudiencePadrao = "fiap-api";
        public const int ExpiracaoPadraoSegundos = 3600;
        public const string EnvIssuer = "JWT_ISSUER";
        public const string EnvAudience = "JWT_AUDIENCE";

        /// <summary>Segredo do token (mínimo 32 caracteres). O lambda-auth não usa <see cref="SsmJwtSecretParameter"/>.</summary>
        public const string EnvSecret = "JWT_SECRET";

        public const string EnvExpiracao = "JWT_EXPIRES_IN_SECONDS";
    }

    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Operador = "Operador";
        public const string Mecanico = "Mecanico";
    }
}

/// <summary>Request enviado para <c>POST /auth/token</c> na <c>lambda-auth-function</c> (lido sem distinção de maiúsculas).</summary>
public sealed record AuthTokenRequest(string Cpf);

/// <summary>Corpo de erro 400/401/500 da <c>lambda-auth-function</c>.</summary>
public sealed record AuthErrorResponse([property: JsonPropertyName("error")] string Error);

/// <summary>
/// Response de <c>POST /auth/token</c> na <c>lambda-auth-function</c>, em snake_case
/// (espelho de <c>Models/AuthResponse.cs</c>). <see cref="TokenType"/> é sempre <c>Bearer</c>.
/// </summary>
public sealed record AuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

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
