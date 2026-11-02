namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>
/// Publicado pelo <c>execution-service</c> quando o mecânico inicia a inspeção técnica do veículo.
/// Consumido pelo <c>os-microservice</c> para atualizar o status da OS para <c>EM_DIAGNOSTICO</c>.
/// </summary>
public sealed record DiagnosticoIniciadoEvent : IntegrationEvent
{
    public required Guid IdMecanico { get; init; }
    public string? NomeMecanico { get; init; }
    public required DateTime DataHora { get; init; }
}

/// <summary>
/// Publicado pelo <c>execution-service</c> quando o mecânico conclui o laudo técnico com serviços e peças sugeridos.
/// Consumido pelo <c>os-microservice</c> para consultar preços no catálogo (<c>fase1-tech-challenge</c>),
/// executar a Saga de reserva de estoque e avançar a OS para <c>AGUARDANDO_APROVACAO</c>.
/// </summary>
public sealed record DiagnosticoTecnicoConcluidoEvent : IntegrationEvent
{
    public required string Laudo { get; init; }
    public required IReadOnlyList<ServicoSugeridoInfo> Servicos { get; init; }
    public required IReadOnlyList<PecaSugeridaInfo> Pecas { get; init; }
    public int? DuracaoDiagnosticoMinutos { get; init; }
}

/// <summary>
/// Publicado de forma idempotente pelo <c>execution-service</c> quando a 1ª tarefa de reparo é iniciada
/// (<c>NA_FILA</c> ➔ <c>EM_EXECUCAO</c>).
/// Consumido pelo <c>os-microservice</c> para atualizar o status da OS para <c>EM_EXECUCAO</c>.
/// </summary>
public sealed record ProducaoIniciadaEvent : IntegrationEvent
{
    public required DateTime DataHoraInicio { get; init; }
}

/// <summary>
/// Publicado pelo <c>execution-service</c> quando 100% das tarefas de reparo são concluídas.
/// Consumido pelo <c>os-microservice</c> para atualizar o status da OS para <c>FINALIZADA</c>.
/// </summary>
public sealed record ProducaoFinalizadaEvent : IntegrationEvent
{
    public required DateTime DataHoraFim { get; init; }
    public required int DuracaoTotalMinutos { get; init; }
}

/// <summary>
/// Publicado pelo <c>execution-service</c> quando a produção é cancelada internamente na oficina (Compensação B).
/// Consumido pelo <c>billing-service</c> (estorno do Pix no Mercado Pago) e pelo <c>os-microservice</c>
/// (cancelamento da OS e estorno das peças ao estoque do Core).
/// </summary>
public sealed record ProducaoCanceladaEvent : IntegrationEvent
{
    public required string Motivo { get; init; }
    public DateTime? DataHoraCancelamento { get; init; }
}

