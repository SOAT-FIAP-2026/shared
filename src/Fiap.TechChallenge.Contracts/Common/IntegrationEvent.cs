using Fiap.TechChallenge.Contracts.Common;

namespace Fiap.TechChallenge.Contracts;

/// <summary>
/// Envelope base de todos os eventos de integração da Saga Coreografada (AWS SNS / SQS).
/// </summary>
/// <remarks>
/// Propaga automaticamente o <c>X-Correlation-ID</c> ativo em <see cref="CorrelationContext.Current"/>
/// (via <see cref="XCorrelationId"/>) e mantém <see cref="CorrelationId"/> com fallback para <see cref="OsId"/>,
/// permitindo rastrear o ciclo de vida completo de uma Ordem de Serviço e cada comunicação entre microsserviços
/// (CloudWatch Logs, Grafana Loki, Grafana Tempo e DynamoDB/PostgreSQL).
/// </remarks>
public abstract record IntegrationEvent
{
    /// <summary>Identificador único do evento (usado para idempotência / Inbox / registro de compensação).</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Instante (UTC) em que o evento ocorreu.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>Versão do contrato do evento.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Identificador da Ordem de Serviço de origem.</summary>
    public required Guid OsId { get; init; }

    /// <summary>
    /// Identificador de correlação textual (<c>X-Correlation-ID</c>) capturado do <see cref="CorrelationContext.Current"/>
    /// ou informado explicitamente ao publicar o evento.
    /// </summary>
    public string? XCorrelationId { get; init; } = CorrelationContext.Current;

    /// <summary>
    /// Identificador de correlação da Saga (quando <see cref="XCorrelationId"/> for um GUID válido, retorna-o;
    /// caso contrário, retorna <see cref="OsId"/>).
    /// </summary>
    public Guid CorrelationId =>
        Guid.TryParse(XCorrelationId, out var parsed) && parsed != Guid.Empty ? parsed : OsId;

    /// <summary>Nome canônico do evento enviado no atributo <c>tipoEvento</c> do AWS SNS.</summary>
    public string TipoEvento => GetType().Name;

    /// <summary>
    /// Retorna o <c>CorrelationId</c> textual efetivo para envio em headers HTTP e atributos de mensagem SNS/SQS.
    /// </summary>
    public string ObterCorrelationId() =>
        !string.IsNullOrWhiteSpace(XCorrelationId)
            ? XCorrelationId
            : (!string.IsNullOrWhiteSpace(CorrelationContext.Current)
                ? CorrelationContext.Current!
                : OsId.ToString());
}

