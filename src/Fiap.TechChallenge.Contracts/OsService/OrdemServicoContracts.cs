namespace Fiap.TechChallenge.Contracts.OsService;

/// <summary>
/// Rotas canônicas da API HTTP de Ordens de Serviço (/api/v1/ordens-servico).
/// </summary>
public static class OrdemServicoApiRoutes
{
    public const string Base = "api/v1/ordens-servico";
    public const string Criar = "api/v1/ordens-servico";
    public const string ObterPorId = "api/v1/ordens-servico/{0}";
    public const string ListarTodas = "api/v1/ordens-servico";
    public const string ObterProgresso = "api/v1/ordens-servico/{0}/progresso";
    public const string IncluirPecaInsumo = "api/v1/ordens-servico/{0}/pecas-insumos";
    public const string RemoverPecaInsumo = "api/v1/ordens-servico/{0}/pecas-insumos/{1}";
    public const string IncluirServico = "api/v1/ordens-servico/{0}/servicos";
    public const string RemoverServico = "api/v1/ordens-servico/{0}/servicos/{1}";
    public const string IniciarDiagnostico = "api/v1/ordens-servico/{0}/iniciar-diagnostico";
    public const string FinalizarDiagnostico = "api/v1/ordens-servico/{0}/finalizar-diagnostico";
    public const string AprovarOrcamento = "api/v1/ordens-servico/{0}/aprovar-orcamento";
    public const string IniciarServico = "api/v1/ordens-servico/{0}/servicos/{1}/iniciar";
    public const string FinalizarServico = "api/v1/ordens-servico/{0}/servicos/{1}/finalizar";
    public const string ConfirmarEntrega = "api/v1/ordens-servico/{0}/confirmar-entrega";
}

public sealed record OrdemServicoRequest(
    Guid ClienteId = default,
    Guid VeiculoId = default,
    string? Observacao = null);

public sealed record OrdemServicoPecaInsumoRequest
{
    public List<Guid> PecasInsumosIds { get; init; } = [];

    public OrdemServicoPecaInsumoRequest() { }
    public OrdemServicoPecaInsumoRequest(List<Guid> pecasInsumosIds) => PecasInsumosIds = pecasInsumosIds;
}

public sealed record OrdemServicoServicosRequest
{
    public List<Guid> ServicosIds { get; init; } = [];

    public OrdemServicoServicosRequest() { }
    public OrdemServicoServicosRequest(List<Guid> servicosIds) => ServicosIds = servicosIds;
}

public sealed record OrdemServicoItemPecaInsumoResponse(
    Guid IdPecaInsumo,
    string Descricao,
    decimal Valor);

public sealed record OrdemServicoItemServicoResponse(
    Guid IdServico,
    DateTime? DataHoraInicio,
    DateTime? DataHoraFim,
    string Descricao,
    decimal Valor);

public sealed record OrdemServicoProgressoServicoResponse(
    Guid IdServico,
    string Nome,
    string Status,
    int TempoEstimadoMinutos,
    int? TempoExecutadoMinutos,
    DateTime? DataHoraInicio,
    DateTime? DataHoraFim);

public sealed record OrdemServicoProgressoResponse(
    Guid IdOrdemServico,
    string Status,
    int PercentualConcluido,
    DateTime DataAbertura,
    DateTime? PrevisaoConclusao,
    IReadOnlyCollection<OrdemServicoProgressoServicoResponse> Servicos);

public sealed record OrdemServicoResponse(
    Guid Id,
    Guid IdCliente,
    Guid IdVeiculo,
    Guid IdStatus,
    string ClienteNome,
    string StatusDescricao,
    string? Observacao,
    DateTime DataAbertura,
    DateTime? DataConclusao,
    decimal? ValorTotal,
    IReadOnlyCollection<OrdemServicoItemServicoResponse> ItensServico,
    IReadOnlyCollection<OrdemServicoItemPecaInsumoResponse> ItensPecaInsumo,
    bool ClienteNotificado = false);
