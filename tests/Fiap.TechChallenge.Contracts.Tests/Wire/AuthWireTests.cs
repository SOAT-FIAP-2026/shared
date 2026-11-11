using System.Text.Json;
using Fiap.TechChallenge.Contracts.AuthService;
using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class AuthWireTests
{
    [Fact]
    public void AuthTokenResponse_espelha_lambda_auth() =>
        WireFixture.DeveEspelhar<AuthTokenResponse>("auth.AuthResponse", WireJsonOptions.PadraoDotNet);

    [Fact]
    public void AuthTokenRequest_espelha_lambda_auth() =>
        WireFixture.DeveEspelhar<AuthTokenRequest>("auth.AuthRequest", WireJsonOptions.PadraoDotNet);

    [Fact]
    public void AuthErrorResponse_serializa_chave_error() =>
        JsonSerializer.Serialize(new AuthErrorResponse("x")).Should().Be("""{"error":"x"}""");

    [Fact]
    public void Constantes_do_lambda_auth()
    {
        JwtAuthConstants.Claims.Sub.Should().Be("sub");
        JwtAuthConstants.Claims.Jti.Should().Be("jti");
        JwtAuthConstants.LambdaAuth.Rota.Should().Be("auth/token");
        JwtAuthConstants.LambdaAuth.IssuerPadrao.Should().Be("fiap-tech-challenge");
        JwtAuthConstants.LambdaAuth.AudiencePadrao.Should().Be("fiap-api");
        JwtAuthConstants.LambdaAuth.ExpiracaoPadraoSegundos.Should().Be(3600);
        JwtAuthConstants.LambdaAuth.EnvIssuer.Should().Be("JWT_ISSUER");
        JwtAuthConstants.LambdaAuth.EnvAudience.Should().Be("JWT_AUDIENCE");
        JwtAuthConstants.LambdaAuth.EnvSecret.Should().Be("JWT_SECRET");
        JwtAuthConstants.LambdaAuth.EnvExpiracao.Should().Be("JWT_EXPIRES_IN_SECONDS");
    }
}
