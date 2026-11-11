namespace Fiap.TechChallenge.Contracts.CoreService;

/// <summary>
/// Rotas HTTP expostas pelo <c>fase1-tech-challenge</c> (Core / Catálogo / Estoque), espelho dos controllers em
/// <c>fase1-tech-challenge/src/Fiap.TechChallenge.Api/Controllers/V1</c>. Consumidas pelo <c>os-microservice</c>
/// (<c>CoreApiClient</c>).
/// </summary>
/// <remarks>
/// Sem barra inicial: o <c>BaseAddress</c> do cliente precisa terminar em <c>/</c>.
/// "Não encontrado" costuma voltar como 400 (<c>DomainException</c>); só <see cref="BuscarClientePorCpfCnpj"/>,
/// <see cref="QuantidadeEstoquePorPeca"/> e <see cref="QuantidadeEstoquePorDescricao"/> devolvem 404.
/// </remarks>
public static class CoreApiRoutes
{
    /// <summary>GET/PUT/DELETE de um cliente.</summary>
    public const string ObterClientePorId = "api/v1/clientes/{0}";

    /// <summary>POST (201 <see cref="IdCriadoResponse"/>) e GET (lista).</summary>
    public const string Clientes = "api/v1/clientes";

    /// <summary>GET anônimo com query <c>?cpfCnpj=</c>; 404 quando não existe.</summary>
    public const string BuscarClientePorCpfCnpj = "api/v1/clientes/buscar-por-cpf-cnpj";

    /// <summary>GET/PATCH/DELETE de um veículo.</summary>
    public const string ObterVeiculoPorId = "api/v1/veiculos/{0}";

    /// <summary>POST (201 <see cref="IdCriadoResponse"/>) e GET paginado (<c>?page=&amp;pageSize=</c>).</summary>
    public const string Veiculos = "api/v1/veiculos";

    /// <summary>GET/PUT/DELETE de um serviço.</summary>
    public const string ObterServicoPorId = "api/v1/servicos/{0}";

    /// <summary>POST (201 <see cref="IdCriadoResponse"/>) e GET (lista).</summary>
    public const string ListarServicos = "api/v1/servicos";

    /// <summary>GET anônimo, lista de <see cref="ServicoMetricaTempoDto"/>.</summary>
    public const string MetricasTempoServicos = "api/v1/servicos/metricas-tempo";

    /// <summary>GET/PUT/DELETE de uma peça/insumo.</summary>
    public const string ObterPecaInsumoPorId = "api/v1/pecas-insumos/{0}";

    /// <summary>POST (201 com guid cru no corpo) e GET (lista).</summary>
    public const string ListarPecasInsumos = "api/v1/pecas-insumos";

    /// <summary>GET com query <c>?descricao=</c>.</summary>
    public const string BuscarPecaInsumoPorDescricao = "api/v1/pecas-insumos/buscar-por-descricao";

    /// <summary>Só POST (201 com guid cru no corpo). Não há GET nesta rota.</summary>
    public const string Estoques = "api/v1/estoques";

    /// <summary>DELETE de estoque; aceita id do estoque ou, como fallback, id da peça.</summary>
    public const string RemoverEstoque = "api/v1/estoques/{0}";

    /// <summary>GET, <c>{0}</c> = idPecaInsumo; devolve número JSON cru ou 404.</summary>
    public const string QuantidadeEstoquePorPeca = "api/v1/estoques/quantidade/peca/{0}";

    /// <summary>GET com query <c>?descricao=</c>; devolve número JSON cru ou 404.</summary>
    public const string QuantidadeEstoquePorDescricao = "api/v1/estoques/quantidade/descricao";

    /// <summary>PUT, <c>{0}</c> = idPecaInsumo; corpo é um número JSON cru (<c>[FromBody] int</c>); sucesso devolve <c>text/plain</c>.</summary>
    public const string RemoverQuantidadeEstoque = "api/v1/estoques/{0}/remover-quantidade";

    /// <summary>PUT, <c>{0}</c> = idPecaInsumo; corpo é um número JSON cru (<c>[FromBody] int</c>); sucesso devolve <c>text/plain</c>.</summary>
    public const string AdicionarQuantidadeEstoque = "api/v1/estoques/{0}/adicionar-quantidade";

    /// <summary>POST anônimo (<see cref="AuthService.StaffLoginRequest"/> → <see cref="AuthService.StaffLoginResponse"/>). Template exato do controller.</summary>
    public const string LoginStaff = "api/v1/Autenticacao/Login";

    /// <summary>POST (Administrador), 201 com guid cru no corpo. Template exato do controller.</summary>
    public const string CadastrarStaff = "api/v1/Autenticacao/Cadastrar";

    /// <summary>GET anônimo (<c>{"message":"pong"}</c>).</summary>
    public const string Ping = "api/ping";
}

/// <summary>Resposta <c>201</c> de criação de clientes, serviços e veículos.</summary>
/// <remarks>Peças/insumos, estoques e cadastro de staff devolvem o guid cru no corpo, sem objeto.</remarks>
public sealed record IdCriadoResponse(Guid Id);

/// <summary>Item de <see cref="CoreApiRoutes.MetricasTempoServicos"/>. Espelho de <c>ServicoMetricaTempoResponse</c> do fase1.</summary>
public sealed record ServicoMetricaTempoDto(
    Guid IdServico,
    string Nome,
    int TempoEstimadoMinutos,
    int? TempoMedioExecucaoMinutos,
    int QuantidadeExecucoes);

/// <summary>Cliente devolvido pelo Core (espelho de <c>ClienteResponse</c> do fase1).</summary>
public sealed record ClienteCatalogoDto(
    Guid Id,
    string Nome,
    string CpfCnpj,
    string Email,
    string Celular);

public sealed record VeiculoCatalogoDto(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    int Ano);

/// <summary>Serviço devolvido pelo Core (espelho de <c>ServicoResponse</c> do fase1).</summary>
public sealed record ServicoCatalogoDto(
    Guid Id,
    string Nome,
    string Descricao,
    decimal ValorUnitario,
    int TempoEstimadoMinutos = 60);

public sealed record PecaInsumoCatalogoDto(
    Guid Id,
    string Descricao,
    decimal ValorUnitario);

public sealed record CriarClienteRequest(
    string Nome,
    string CpfCnpj,
    string Email,
    string Celular);

public sealed record AtualizarClienteRequest(
    string Nome,
    string CpfCnpj,
    string Email,
    string Celular);

public sealed record ClienteDetalhadoDto(
    Guid Id,
    string Nome,
    string CpfCnpj,
    string Email,
    string Celular);

public sealed record CriarVeiculoRequest(
    string Placa,
    string Marca,
    string Modelo,
    int Ano);

public sealed record AtualizarVeiculoPatchRequest(
    string? Placa = null,
    string? Marca = null,
    string? Modelo = null,
    int? Ano = null);

public sealed record CriarServicoRequest(
    string Nome,
    string Descricao,
    decimal ValorUnitario,
    int TempoEstimadoMinutos = 60);

public sealed record CriarPecaInsumoRequest(
    string Descricao,
    decimal ValorUnitario);

public sealed record CriarEstoqueRequest(
    Guid IdPecaInsumo,
    int Quantidade);

/// <remarks>Não trafega hoje: os endpoints de ajuste recebem um número JSON cru (<c>[FromBody] int</c>); este objeto (<c>{"quantidade":n}</c>) seria rejeitado com 400.</remarks>
public sealed record AjustarQuantidadeEstoqueRequest(
    int Quantidade);

/// <remarks>Não trafega hoje: nenhum endpoint do Core devolve estoque detalhado.</remarks>
public sealed record EstoqueDetalhadoDto(
    Guid Id,
    Guid IdPecaInsumo,
    int Quantidade);
