using System.Text.Json;
using Fiap.TechChallenge.Contracts.CoreService;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class CoreContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ClienteCatalogoDto_deve_suportar_telefone_opcional_e_roundtrip_json()
    {
        var id = Guid.NewGuid();
        var dto = new ClienteCatalogoDto(id, "Carlos Souza", "carlos@email.com", "+5511988887777", "+551133334444");

        dto.Id.Should().Be(id);
        dto.Nome.Should().Be("Carlos Souza");
        dto.Email.Should().Be("carlos@email.com");
        dto.Celular.Should().Be("+5511988887777");
        dto.Telefone.Should().Be("+551133334444");

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var copy = JsonSerializer.Deserialize<ClienteCatalogoDto>(json, JsonOptions);

        copy.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void ServicoCatalogoDto_deve_suportar_descricao_opcional_e_roundtrip_json()
    {
        var id = Guid.NewGuid();
        var dto = new ServicoCatalogoDto(id, "Alinhamento 3D", 150.00m, 45, "Alinhamento computadorizado com tecnologia 3D");

        dto.Id.Should().Be(id);
        dto.Nome.Should().Be("Alinhamento 3D");
        dto.ValorUnitario.Should().Be(150.00m);
        dto.TempoEstimadoMinutos.Should().Be(45);
        dto.Descricao.Should().Be("Alinhamento computadorizado com tecnologia 3D");

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var copy = JsonSerializer.Deserialize<ServicoCatalogoDto>(json, JsonOptions);

        copy.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void VeiculoCatalogoDto_e_PecaInsumoCatalogoDto_devem_inicializar_corretamente()
    {
        var veiculoId = Guid.NewGuid();
        var veiculo = new VeiculoCatalogoDto(veiculoId, "XYZ9A88", "Toyota", "Corolla", 2021);
        veiculo.Placa.Should().Be("XYZ9A88");
        veiculo.Marca.Should().Be("Toyota");
        veiculo.Modelo.Should().Be("Corolla");
        veiculo.Ano.Should().Be(2021);

        var pecaId = Guid.NewGuid();
        var peca = new PecaInsumoCatalogoDto(pecaId, "Pastilha de Freio Dianteira", 220.50m);
        peca.Id.Should().Be(pecaId);
        peca.Descricao.Should().Be("Pastilha de Freio Dianteira");
        peca.ValorUnitario.Should().Be(220.50m);
    }

    [Fact]
    public void Request_e_Response_DTOs_do_Core_devem_serializar_corretamente()
    {
        var criarCliente = new CriarClienteRequest("Mariana", "12345678901", "mariana@email.com", "11999998888");
        var atualizarCliente = new AtualizarClienteRequest("Mariana Silva", "12345678901", "mariana.silva@email.com", "11999998888");
        var detalhadoCliente = new ClienteDetalhadoDto(Guid.NewGuid(), "Mariana Silva", "12345678901", "mariana.silva@email.com", "11999998888");

        criarCliente.Nome.Should().Be("Mariana");
        atualizarCliente.Email.Should().Be("mariana.silva@email.com");
        detalhadoCliente.CpfCnpj.Should().Be("12345678901");

        var criarVeiculo = new CriarVeiculoRequest("ABC1234", "Ford", "Ka", 2019);
        var patchVeiculo = new AtualizarVeiculoPatchRequest(Placa: "ABC1234", Modelo: "Ka Sedan");
        criarVeiculo.Marca.Should().Be("Ford");
        patchVeiculo.Modelo.Should().Be("Ka Sedan");
        patchVeiculo.Ano.Should().BeNull();

        var criarServico = new CriarServicoRequest("Balanceamento", "Balanceamento de rodas", 80.00m, 30);
        criarServico.TempoEstimadoMinutos.Should().Be(30);

        var criarPeca = new CriarPecaInsumoRequest("Filtro de Ar", 45.00m);
        criarPeca.Descricao.Should().Be("Filtro de Ar");

        var pecaId = Guid.NewGuid();
        var criarEstoque = new CriarEstoqueRequest(pecaId, 10);
        var ajustarEstoque = new AjustarQuantidadeEstoqueRequest(5);
        var detalhadoEstoque = new EstoqueDetalhadoDto(Guid.NewGuid(), pecaId, 15);

        criarEstoque.Quantidade.Should().Be(10);
        ajustarEstoque.Quantidade.Should().Be(5);
        detalhadoEstoque.Quantidade.Should().Be(15);

        // Roundtrip JSON
        var json = JsonSerializer.Serialize(detalhadoCliente, JsonOptions);
        var copy = JsonSerializer.Deserialize<ClienteDetalhadoDto>(json, JsonOptions);
        copy.Should().BeEquivalentTo(detalhadoCliente);
    }

    [Fact]
    public void CoreApiRoutes_devem_conter_formatos_corretos()
    {
        var id = Guid.NewGuid();
        string.Format(CoreApiRoutes.ObterClientePorId, id).Should().Be($"api/v1/clientes/{id}");
        string.Format(CoreApiRoutes.ObterVeiculoPorId, id).Should().Be($"api/v1/veiculos/{id}");
        string.Format(CoreApiRoutes.ObterServicoPorId, id).Should().Be($"api/v1/servicos/{id}");
        CoreApiRoutes.ListarServicos.Should().Be("api/v1/servicos");
        string.Format(CoreApiRoutes.ObterPecaInsumoPorId, id).Should().Be($"api/v1/pecas-insumos/{id}");
        CoreApiRoutes.ListarPecasInsumos.Should().Be("api/v1/pecas-insumos");
        string.Format(CoreApiRoutes.RemoverQuantidadeEstoque, id).Should().Be($"api/v1/estoques/{id}/remover-quantidade");
        string.Format(CoreApiRoutes.AdicionarQuantidadeEstoque, id).Should().Be($"api/v1/estoques/{id}/adicionar-quantidade");
        CoreApiRoutes.LoginStaff.Should().Be("api/v1/autenticacao/login");
        CoreApiRoutes.CadastrarStaff.Should().Be("api/v1/autenticacao/cadastrar");
    }
}
