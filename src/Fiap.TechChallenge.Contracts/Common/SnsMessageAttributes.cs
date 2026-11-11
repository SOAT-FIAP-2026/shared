namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Constantes padronizadas de MessageAttributes e origens para o tópico AWS SNS
/// (<c>techchallenge-eventos-&lt;stage&gt;</c>) e filas AWS SQS.
/// </summary>
public static class SnsMessageAttributes
{
    /// <summary>Atributo obrigatório usado na FilterPolicy das assinaturas SNS -> SQS/Lambda.</summary>
    public const string TipoEvento = "tipoEvento";

    /// <summary>Identificador do microsserviço emissor da mensagem.</summary>
    public const string Origem = "origem";

    /// <summary>ID da Ordem de Serviço para rastreamento e filtro.</summary>
    public const string IdOrdemServico = "idOrdemServico";

    /// <summary>Header/atributo de correlação ponta a ponta.</summary>
    public const string CorrelationId = "X-Correlation-ID";

    /// <summary>
    /// Atributo SQS da fila de compensação do <c>os-microservice</c> (<c>SqsFilaCompensacao</c>) com o
    /// <c>TipoCompensacaoContrato</c> como texto. Essa fila não usa <see cref="TipoEvento"/> nem <see cref="Origem"/>.
    /// </summary>
    public const string TipoCompensacao = "tipo";

    public static class Origens
    {
        public const string OsService = "techchallenge.ordem-servico";
        public const string ExecutionService = "techchallenge.execucao";
        public const string BillingService = "techchallenge.pagamento";
        public const string CoreEstoqueService = "techchallenge.estoque";
        public const string LambdaAuth = "techchallenge.auth";
    }
}

