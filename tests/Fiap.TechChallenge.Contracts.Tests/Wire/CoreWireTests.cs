using Fiap.TechChallenge.Contracts.AuthService;
using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.CoreService;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class CoreWireTests
{
    public static TheoryData<string, Type> Dtos => new()
    {
        { "core.ClienteResponse", typeof(ClienteCatalogoDto) },
        { "core.ClienteResponse", typeof(ClienteDetalhadoDto) },
        { "core.VeiculoResponse", typeof(VeiculoCatalogoDto) },
        { "core.ServicoResponse", typeof(ServicoCatalogoDto) },
        { "core.PecaInsumoResponse", typeof(PecaInsumoCatalogoDto) },
        { "core.ServicoMetricaTempoResponse", typeof(ServicoMetricaTempoDto) },
        { "core.EstoqueResponse", typeof(EstoqueDetalhadoDto) },
        { "core.PagedResult", typeof(PagedResultContrato<VeiculoCatalogoDto>) },
        { "core.ClienteRequest", typeof(CriarClienteRequest) },
        { "core.ClienteRequest", typeof(AtualizarClienteRequest) },
        { "core.VeiculoRequest", typeof(CriarVeiculoRequest) },
        { "core.VeiculoPatchRequest", typeof(AtualizarVeiculoPatchRequest) },
        { "core.ServicoRequest", typeof(CriarServicoRequest) },
        { "core.PecaInsumoRequest", typeof(CriarPecaInsumoRequest) },
        { "core.EstoqueRequest", typeof(CriarEstoqueRequest) },
        { "core.LoginRequest", typeof(StaffLoginRequest) },
        { "core.LoginResponse", typeof(StaffLoginResponse) },
        { "core.CadastrarRequest", typeof(StaffCadastrarRequest) },
    };

    [Theory]
    [MemberData(nameof(Dtos))]
    public void Dto_do_Core_espelha_fase1(string fixture, Type tipo) =>
        WireFixture.DeveEspelhar(tipo, fixture, WireJsonOptions.Http);

    [Fact]
    public void IdCriadoResponse_tem_so_id() =>
        WireShape.Extrair(typeof(IdCriadoResponse), WireJsonOptions.Http)
            .Select(p => p.Key).Should().Equal("id");

    [Fact]
    public void Rotas_do_Core_espelham_controllers()
    {
        CoreApiRoutes.Clientes.Should().Be("api/v1/clientes");
        CoreApiRoutes.ObterClientePorId.Should().Be("api/v1/clientes/{0}");
        CoreApiRoutes.BuscarClientePorCpfCnpj.Should().Be("api/v1/clientes/buscar-por-cpf-cnpj");
        CoreApiRoutes.Veiculos.Should().Be("api/v1/veiculos");
        CoreApiRoutes.ObterVeiculoPorId.Should().Be("api/v1/veiculos/{0}");
        CoreApiRoutes.ListarServicos.Should().Be("api/v1/servicos");
        CoreApiRoutes.ObterServicoPorId.Should().Be("api/v1/servicos/{0}");
        CoreApiRoutes.MetricasTempoServicos.Should().Be("api/v1/servicos/metricas-tempo");
        CoreApiRoutes.ListarPecasInsumos.Should().Be("api/v1/pecas-insumos");
        CoreApiRoutes.ObterPecaInsumoPorId.Should().Be("api/v1/pecas-insumos/{0}");
        CoreApiRoutes.BuscarPecaInsumoPorDescricao.Should().Be("api/v1/pecas-insumos/buscar-por-descricao");
        CoreApiRoutes.Estoques.Should().Be("api/v1/estoques");
        CoreApiRoutes.RemoverEstoque.Should().Be("api/v1/estoques/{0}");
        CoreApiRoutes.QuantidadeEstoquePorPeca.Should().Be("api/v1/estoques/quantidade/peca/{0}");
        CoreApiRoutes.QuantidadeEstoquePorDescricao.Should().Be("api/v1/estoques/quantidade/descricao");
        CoreApiRoutes.RemoverQuantidadeEstoque.Should().Be("api/v1/estoques/{0}/remover-quantidade");
        CoreApiRoutes.AdicionarQuantidadeEstoque.Should().Be("api/v1/estoques/{0}/adicionar-quantidade");
        CoreApiRoutes.LoginStaff.Should().Be("api/v1/Autenticacao/Login");
        CoreApiRoutes.CadastrarStaff.Should().Be("api/v1/Autenticacao/Cadastrar");
        CoreApiRoutes.Ping.Should().Be("api/ping");
    }
}
