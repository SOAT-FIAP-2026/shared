namespace Fiap.TechChallenge.Contracts.ExecutionService;

/// <summary>
/// Rotas canônicas da API HTTP de Execução Técnica e Chão de Oficina (/api/v1/producao).
/// </summary>
public static class ExecutionApiRoutes
{
    public const string Base = "api/v1/producao";
    public const string FilaProducao = "api/v1/producao/fila";
    public const string DiagnosticosPendentes = "api/v1/producao/diagnosticos/pendentes";
    public const string ObterPorId = "api/v1/producao/{0}";
    public const string ObterPorOrdemServicoId = "api/v1/producao/ordem-servico/{0}";
    public const string IniciarDiagnostico = "api/v1/producao/{0}/diagnostico/iniciar";
    public const string FinalizarDiagnostico = "api/v1/producao/{0}/diagnostico/finalizar";
    public const string IniciarTarefa = "api/v1/producao/{0}/tarefas/{1}/iniciar";
    public const string FinalizarTarefa = "api/v1/producao/{0}/tarefas/{1}/finalizar";
    public const string CancelarProducao = "api/v1/producao/{0}/cancelar";
    public const string MetricasProdutividade = "api/v1/producao/metricas/produtividade";
}

public sealed record FinalizarDiagnosticoRequest(
    string LaudoTecnico = "",
    IReadOnlyList<ServicoSugeridoRequest>? ServicosSugeridos = null,
    IReadOnlyList<PecaSugeridaRequest>? PecasSugeridas = null);

public sealed record ServicoSugeridoRequest(
    Guid IdServico,
    string? Descricao = null);

public sealed record PecaSugeridaRequest(
    Guid IdPeca,
    int Quantidade,
    string? Descricao = null);

public sealed record FinalizarTarefaRequest(
    string? Observacoes = null);

public sealed record CancelarProducaoRequest(
    string Motivo = "");
