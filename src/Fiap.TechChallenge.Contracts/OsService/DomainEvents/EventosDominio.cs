using Fiap.TechChallenge.Contracts.Common;

namespace Fiap.TechChallenge.Contracts.OsService.DomainEvents;

/// <summary>
/// Envelope dos eventos de domínio que o <c>os-microservice</c> publica no SNS via outbox (DynamoDB Streams).
/// Espelho de <c>os-microservice/src/OrdemServico.Lambda/Domain/Events/EventosDominio.cs</c>.
/// </summary>
/// <remarks>
/// Gravado em <c>OrdemServicoRepository</c> e publicado por <c>OutboxPublisherFunction</c> com
/// <see cref="WireJsonOptions.OsEventos"/> (camelCase, nulos omitidos). O atributo SNS <c>tipoEvento</c>
/// é o nome do tipo, sem sufixo <c>Event</c>. Convive no mesmo tópico com <see cref="IntegrationEvent"/>,
/// que usa outros nomes de campo (<c>osId</c>/<c>eventId</c>/<c>occurredAt</c> em vez de
/// <c>idOrdemServico</c>/<c>idEvento</c>/<c>ocorridoEm</c>).
/// </remarks>
public abstract record EventoDominio(Guid IdOrdemServico)
{
    public Guid IdEvento { get; init; } = Guid.NewGuid();
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
    public string TipoEvento => GetType().Name;
    public string? CorrelationId { get; init; } = CorrelationContext.Current;
}

/// <summary>OS aberta. <see cref="Status"/> é o código do status (ex.: <c>RECEBIDA</c>).</summary>
public sealed record OrdemServicoCriada(Guid IdOrdemServico, Guid IdCliente, Guid IdVeiculo, string Status)
    : EventoDominio(IdOrdemServico);

/// <summary>Mudança de status da OS; valores são códigos de status.</summary>
public sealed record StatusOrdemServicoAlterado(Guid IdOrdemServico, string StatusAnterior, string StatusAtual)
    : EventoDominio(IdOrdemServico);

/// <summary>Peças/insumos incluídos na OS pela saga de reserva de estoque.</summary>
public sealed record PecasInsumoIncluidas(Guid IdOrdemServico, Guid IdSaga, IReadOnlyCollection<Guid> PecasInsumosIds)
    : EventoDominio(IdOrdemServico);

/// <summary>Gatilho de compensação: as peças baixadas do estoque precisam ser devolvidas.</summary>
public sealed record OrcamentoReprovado(Guid IdOrdemServico, IReadOnlyCollection<Guid> PecasInsumosIds)
    : EventoDominio(IdOrdemServico);

/// <summary>Gatilho de compensação: a OS foi cancelada e as peças precisam ser devolvidas.</summary>
public sealed record OrdemServicoCancelada(Guid IdOrdemServico, string Motivo, IReadOnlyCollection<Guid> PecasInsumosIds)
    : EventoDominio(IdOrdemServico);

/// <summary>
/// Compensação concluída. <see cref="Tipo"/> é o nome de <c>TipoCompensacaoContrato</c>;
/// <see cref="Origem"/> é <c>sqs</c>, <c>sqs:os-worker</c> ou <c>sns:{origem ou TopicArn}</c>.
/// </summary>
public sealed record CompensacaoExecutada(Guid IdOrdemServico, string IdCompensacao, string Tipo, string Origem)
    : EventoDominio(IdOrdemServico);
