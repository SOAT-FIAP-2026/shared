namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Contexto assíncrono (<see cref="AsyncLocal{T}"/>) para propagação ponta a ponta do <c>X-Correlation-ID</c>
/// entre requisições HTTP, workers em background (SQS/SNS) e funções serverless (AWS Lambda).
/// </summary>
public static class CorrelationContext
{
    /// <summary>Nome canônico do header HTTP e atributo de mensagem SNS/SQS (<c>X-Correlation-ID</c>).</summary>
    public const string HeaderName = SnsMessageAttributes.CorrelationId;

    /// <summary>Chave estruturada de log (<c>correlation_id</c>) utilizada no Loki e CloudWatch Logs.</summary>
    public const string LogScopeKey = "correlation_id";

    /// <summary>Chave estruturada de log (<c>os_id</c>) para rastreamento por Ordem de Serviço.</summary>
    public const string OsIdScopeKey = "os_id";

    private static readonly AsyncLocal<string?> CurrentId = new();

    /// <summary>Obtém ou define o <c>CorrelationId</c> ativo no fluxo assíncrono atual.</summary>
    public static string? Current
    {
        get => CurrentId.Value;
        set => CurrentId.Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Abre um escopo temporário com o <paramref name="correlationId"/> informado e restaura o valor anterior ao dispor.
    /// </summary>
    public static IDisposable Push(string? correlationId)
    {
        var previous = CurrentId.Value;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            CurrentId.Value = correlationId.Trim();
        }

        return new Scope(previous);
    }

    /// <summary>
    /// Resolve o <c>CorrelationId</c> efetivo priorizando o valor explícito, depois o contexto assíncrono atual
    /// e por fim um valor de fallback (como o ID da Ordem de Serviço ou um novo GUID).
    /// </summary>
    public static string Resolve(string? explicitCorrelationId = null, string? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitCorrelationId))
            return explicitCorrelationId.Trim();

        if (!string.IsNullOrWhiteSpace(CurrentId.Value))
            return CurrentId.Value!;

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();

        return Guid.NewGuid().ToString();
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            CurrentId.Value = previous;
            _disposed = true;
        }
    }
}

