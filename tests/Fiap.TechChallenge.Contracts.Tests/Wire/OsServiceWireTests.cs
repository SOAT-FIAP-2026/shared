using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.OsService;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class OsServiceWireTests
{
    [Fact]
    public void Catalogo_de_status_espelha_StatusOS()
    {
        var esperado = WireFixture.Ler("os.StatusOS")["valores"]!.AsArray()
            .Select(v => new StatusOrdemServicoInfo(
                Guid.Parse(v!["id"]!.GetValue<string>()),
                v["codigo"]!.GetValue<string>(),
                v["descricao"]!.GetValue<string>()))
            .ToList();

        OrdemServicoStatus.Catalogo.Should().Equal(esperado);
    }

    [Fact]
    public void Codigos_do_catalogo_existem_como_constantes()
    {
        var constantes = typeof(OrdemServicoStatus).GetFields()
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        constantes.Should().Contain(OrdemServicoStatus.Catalogo.Select(s => s.Codigo));
    }

    [Fact]
    public void Rotas_extras_do_OS()
    {
        OrdemServicoApiRoutes.Health.Should().Be("api/v1/ordens-servico/health");
        OrdemServicoApiRoutes.ParametroAprovado.Should().Be("aprovado");
    }

    [Fact]
    public void OrdemServicoCriadaResponse_tem_chaves_id_e_clienteNotificado()
    {
        var forma = WireShape.Extrair(typeof(OrdemServicoCriadaResponse), WireJsonOptions.Http);

        forma.Select(p => p.Key).Should().Equal("clienteNotificado", "id");
        forma["clienteNotificado"]!.GetValue<string>().Should().Be("boolean");
        forma["id"]!.GetValue<string>().Should().Be("string");
    }
}
