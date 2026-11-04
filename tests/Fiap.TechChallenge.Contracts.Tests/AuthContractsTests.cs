using System.Text.Json;
using Fiap.TechChallenge.Contracts.AuthService;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class AuthContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void JwtAuthConstants_deve_conter_valores_padronizados()
    {
        JwtAuthConstants.DefaultIssuer.Should().Be("TechChallenge");
        JwtAuthConstants.DefaultAudience.Should().Be("techchallenge.com.br");
        JwtAuthConstants.SsmJwtSecretParameter.Should().Be("/techchallenge/prod/jwt_secret");
        JwtAuthConstants.CorrelationIdHeader.Should().Be("X-Correlation-ID");

        JwtAuthConstants.Claims.UsuarioId.Should().Be("UsuarioId");
        JwtAuthConstants.Claims.Cpf.Should().Be("cpf");

        JwtAuthConstants.Roles.Administrador.Should().Be("Administrador");
        JwtAuthConstants.Roles.Operador.Should().Be("Operador");
        JwtAuthConstants.Roles.Mecanico.Should().Be("Mecanico");
    }

    [Fact]
    public void Staff_Auth_DTOs_devem_inicializar_e_serializar_corretamente()
    {
        var loginReq = new StaffLoginRequest("admin", "Senha@123");
        loginReq.Login.Should().Be("admin");
        loginReq.Senha.Should().Be("Senha@123");

        var loginRes = new StaffLoginResponse("jwt-token-xyz", "Administrador");
        loginRes.Token.Should().Be("jwt-token-xyz");
        loginRes.NomeUsuario.Should().Be("Administrador");

        var permissaoId = Guid.NewGuid();
        var cadReq = new StaffCadastrarRequest("Mário", "mario@oficina.com", "mario.mecanico", "Mecanico#2026", permissaoId);
        cadReq.Nome.Should().Be("Mário");
        cadReq.Email.Should().Be("mario@oficina.com");
        cadReq.Login.Should().Be("mario.mecanico");
        cadReq.Senha.Should().Be("Mecanico#2026");
        cadReq.IdPermissao.Should().Be(permissaoId);

        var json = JsonSerializer.Serialize(cadReq, JsonOptions);
        var copy = JsonSerializer.Deserialize<StaffCadastrarRequest>(json, JsonOptions);
        copy.Should().BeEquivalentTo(cadReq);
    }

    [Fact]
    public void AuthTokenRequest_e_Response_devem_funcionar_corretamente()
    {
        var req = new AuthTokenRequest("12345678909");
        req.Cpf.Should().Be("12345678909");

        var res = new AuthTokenResponse("token-abc", "Bearer", 7200);
        res.AccessToken.Should().Be("token-abc");
        res.TokenType.Should().Be("Bearer");
        res.ExpiresIn.Should().Be(7200);
    }
}
