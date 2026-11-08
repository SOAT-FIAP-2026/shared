namespace Fiap.TechChallenge.Contracts.ExecutionService;

/// <summary>
/// Rotas da API HTTP do <c>execution-service</c> (/api/v1/producao), espelho de <c>ProducaoController</c>.
/// Respostas em <see cref="Responses"/>.
/// </summary>
public static class ExecutionApiRoutes
{
    public const string Base = "api/v1/producao";

    /// <summary>GET, query <see cref="ParametroStatus"/> e <see cref="ParametroFilialId"/>.</summary>
    public const string FilaProducao = "api/v1/producao/fila";

    /// <summary>GET, query <see cref="ParametroFilialId"/>.</summary>
    public const string DiagnosticosPendentes = "api/v1/producao/diagnosticos/pendentes";

    public const string ObterPorId = "api/v1/producao/{0}";
    public const string ObterPorOrdemServicoId = "api/v1/producao/ordem-servico/{0}";

    /// <summary><c>PATCH</c>, sem corpo.</summary>
    public const string IniciarDiagnostico = "api/v1/producao/{0}/diagnostico/iniciar";

    /// <summary><c>POST</c>, corpo <see cref="FinalizarDiagnosticoRequest"/>.</summary>
    public const string FinalizarDiagnostico = "api/v1/producao/{0}/diagnostico/finalizar";

    /// <summary><c>PATCH</c>, sem corpo.</summary>
    public const string IniciarTarefa = "api/v1/producao/{0}/tarefas/{1}/iniciar";

    /// <summary><c>PATCH</c>, corpo opcional <see cref="FinalizarTarefaRequest"/>.</summary>
    public const string FinalizarTarefa = "api/v1/producao/{0}/tarefas/{1}/finalizar";

    /// <summary><c>POST</c>, corpo <see cref="CancelarProducaoRequest"/> (Administrador).</summary>
    public const string CancelarProducao = "api/v1/producao/{0}/cancelar";

    /// <summary>GET (Administrador), query <see cref="ParametroDe"/>, <see cref="ParametroAte"/>, <see cref="ParametroIdMecanico"/>.</summary>
    public const string MetricasProdutividade = "api/v1/producao/metricas/produtividade";

    /// <summary>Filtro de status da fila: nome PascalCase, número ou UPPER_SNAKE.</summary>
    public const string ParametroStatus = "status";
    public const string ParametroFilialId = "filialId";
    public const string ParametroDe = "de";
    public const string ParametroAte = "ate";
    public const string ParametroIdMecanico = "idMecanico";
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
