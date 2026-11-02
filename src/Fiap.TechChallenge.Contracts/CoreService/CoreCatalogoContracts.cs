namespace Fiap.TechChallenge.Contracts.CoreService;

/// <summary>
/// Contratos HTTP REST síncronos expostos pelo <c>fase1-tech-challenge</c> (Core / Catálogo / Estoque)
/// e consumidos pelo <c>os-microservice</c> (<c>CoreApiClient</c>) e demais serviços.
/// </summary>
public static class CoreApiRoutes
{
    public const string ObterClientePorId = "api/v1/clientes/{0}";
    public const string ObterVeiculoPorId = "api/v1/veiculos/{0}";
    public const string ObterServicoPorId = "api/v1/servicos/{0}";
    public const string ListarServicos = "api/v1/servicos";
    public const string ObterPecaInsumoPorId = "api/v1/pecas-insumos/{0}";
    public const string ListarPecasInsumos = "api/v1/pecas-insumos";
    public const string RemoverQuantidadeEstoque = "api/v1/estoques/{0}/remover-quantidade";
    public const string AdicionarQuantidadeEstoque = "api/v1/estoques/{0}/adicionar-quantidade";
    public const string LoginStaff = "api/v1/autenticacao/login";
}

public sealed record ClienteCatalogoDto(
    Guid Id,
    string Nome,
    string Email,
    string? Celular = null);

public sealed record VeiculoCatalogoDto(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    int Ano);

public sealed record ServicoCatalogoDto(
    Guid Id,
    string Nome,
    decimal ValorUnitario,
    int TempoEstimadoMinutos);

public sealed record PecaInsumoCatalogoDto(
    Guid Id,
    string Descricao,
    decimal ValorUnitario);

