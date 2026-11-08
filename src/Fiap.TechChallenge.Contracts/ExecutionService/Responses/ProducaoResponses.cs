namespace Fiap.TechChallenge.Contracts.ExecutionService.Responses;

// Espelho de execution-service/src/Fiap.TechChallenge.Execution.Application/DTOs/ProducaoResponses.cs.
// Sub-namespace próprio porque o execution-service tem tipos com os mesmos nomes e importa
// Fiap.TechChallenge.Contracts.ExecutionService no ProducaoController.
// Campos Status são códigos UPPER_SNAKE (ver StatusOrdemProducaoContrato e StatusTarefaProducaoContrato).

/// <summary>Ordem de produção completa (GET por id, por OS e retorno das ações).</summary>
public sealed record OrdemProducaoResponse(
    Guid Id,
    Guid OrdemServicoId,
    string? FilialId,
    string Status,
    int Versao,
    ClienteSnapshotResponse Cliente,
    VeiculoSnapshotResponse Veiculo,
    DiagnosticoResponse? Diagnostico,
    DateTime DataAbertura,
    DateTime? DataLiberacao,
    DateTime? DataConclusao,
    int DuracaoTotalTarefasMinutos,
    IReadOnlyList<TarefaProducaoResponse> Tarefas,
    IReadOnlyList<HistoricoProducaoResponse> Historico);

/// <summary>Snapshot do cliente recebido em <c>OrdemServicoAbertaEvent</c>.</summary>
public sealed record ClienteSnapshotResponse(
    Guid IdCliente,
    string Nome,
    string? Telefone);

/// <summary>Snapshot do veículo recebido em <c>OrdemServicoAbertaEvent</c>.</summary>
public sealed record VeiculoSnapshotResponse(
    string Placa,
    string Modelo,
    int Ano);

/// <summary>Diagnóstico técnico da ordem de produção.</summary>
public sealed record DiagnosticoResponse(
    Guid Id,
    Guid IdMecanico,
    string NomeMecanico,
    DateTime DataHoraInicio,
    DateTime? DataHoraFim,
    int? DuracaoMinutos,
    string? LaudoTecnico,
    IReadOnlyList<ServicoSugeridoResponse> ServicosSugeridos,
    IReadOnlyList<PecaSugeridaResponse> PecasSugeridas);

/// <summary>Serviço sugerido no diagnóstico.</summary>
public sealed record ServicoSugeridoResponse(
    Guid IdServico,
    string? Descricao);

/// <summary>Peça sugerida no diagnóstico.</summary>
public sealed record PecaSugeridaResponse(
    Guid IdPeca,
    int Quantidade,
    string? Descricao);

/// <summary>Tarefa de produção. <see cref="Status"/> usa <see cref="StatusTarefaProducaoContrato"/>.</summary>
public sealed record TarefaProducaoResponse(
    Guid Id,
    Guid IdServicoOrigem,
    string Descricao,
    int TempoEstimadoMinutos,
    int? DuracaoRealMinutos,
    Guid? IdMecanicoResponsavel,
    string? NomeMecanicoResponsavel,
    string Status,
    DateTime? DataHoraInicio,
    DateTime? DataHoraFim,
    string? ObservacoesTecnicas);

/// <summary>Entrada do histórico de status da ordem de produção.</summary>
public sealed record HistoricoProducaoResponse(
    Guid Id,
    string Status,
    string Descricao,
    DateTime DataHora);

/// <summary>Item da fila de produção e da lista de diagnósticos pendentes.</summary>
public sealed record OrdemProducaoFilaItemResponse(
    Guid Id,
    Guid OrdemServicoId,
    string? FilialId,
    string Status,
    ClienteSnapshotResponse Cliente,
    VeiculoSnapshotResponse Veiculo,
    DateTime DataAbertura,
    DateTime? DataLiberacao,
    int TotalTarefas,
    int TarefasConcluidas,
    int TempoTotalEstimadoMinutos);

/// <summary>Relatório de <c>metricas/produtividade</c>.</summary>
public sealed record RelatorioProdutividadeResponse(
    DateTime? PeriodoInicio,
    DateTime? PeriodoFim,
    int TotalTarefasConcluidas,
    IReadOnlyList<MetricaProdutividadeMecanicoResponse> Mecanicos);

/// <summary>Métricas de um mecânico no relatório de produtividade.</summary>
public sealed record MetricaProdutividadeMecanicoResponse(
    Guid IdMecanico,
    string NomeMecanico,
    int TarefasConcluidas,
    int TempoTotalEstimadoMinutos,
    int TempoTotalRealizadoMinutos,
    double MediaEstimadaMinutos,
    double MediaRealizadaMinutos,
    double IndiceEficienciaPercentual);
