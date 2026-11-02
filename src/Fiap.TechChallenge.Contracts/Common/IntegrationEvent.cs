namespace Fiap.TechChallenge.Contracts;

/// <summary>
/// Envelope base de todos os eventos de integração da Saga Coreografada (AWS SNS / SQS).
/// </summary>
/// <remarks>
/// O <see cref="CorrelationId"/> é sempre igual ao <see cref="OsId"/>, permitindo
/// rastrear o ciclo de vida completo de uma Ordem de Serviço entre todos os serviços
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

    /// <summary>Identificador de correlação da Saga (sempre igual a <see cref="OsId"/>).</summary>
    public Guid CorrelationId => OsId;

    /// <summary>Nome canônico do evento enviado no atributo <c>tipoEvento</c> do AWS SNS.</summary>
    public string TipoEvento => GetType().Name;
}

