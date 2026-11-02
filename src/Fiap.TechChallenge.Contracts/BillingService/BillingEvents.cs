namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>
/// Publicado pelo <c>billing-service</c> quando o orçamento e o QR Code/Link do Mercado Pago são gerados
/// e enviados ao cliente.
/// </summary>
public sealed record OrcamentoGeradoEvent : IntegrationEvent
{
    public required Guid OrcamentoId { get; init; }
    public required decimal ValorTotal { get; init; }
    public string? LinkPagamento { get; init; }
    public string? QrCodePix { get; init; }
}

/// <summary>
/// Publicado pelo <c>billing-service</c> após confirmação do pagamento pelo Mercado Pago.
/// Consumido pelo <c>os-microservice</c> para atualizar o status da OS para <c>ORCAMENTO_APROVADO</c>
/// e publicar <see cref="OrdemServicoLiberadaParaExecucaoEvent"/>.
/// </summary>
public sealed record PagamentoConfirmadoEvent : IntegrationEvent
{
    public required Guid PagamentoId { get; init; }
    public required decimal Valor { get; init; }
}

/// <summary>
/// Publicado pelo <c>billing-service</c> quando o pagamento expira, é recusado no Mercado Pago
/// ou o orçamento é reprovado (Compensação A).
/// Consumido pelo <c>os-microservice</c> (<c>CompensacaoEventoFunction</c>) para cancelar a OS
/// e devolver as peças reservadas ao estoque do Core.
/// </summary>
public sealed record PagamentoCanceladoCompensacaoEvent : IntegrationEvent
{
    public required string Motivo { get; init; }
}

/// <summary>
/// Evento compatível com o filtro <c>tipoEvento = PagamentoRecusado</c> já assinado pela
/// <c>CompensacaoEventoFunction</c> do <c>os-microservice</c> no tópico SNS.
/// </summary>
public sealed record PagamentoRecusadoEvent : IntegrationEvent
{
    public required string Motivo { get; init; }
}

/// <summary>
/// Publicado pelo <c>billing-service</c> após efetuar o estorno (refund) no Mercado Pago em resposta
/// a <see cref="ProducaoCanceladaEvent"/> (Compensação B).
/// Consumido pelo <c>os-microservice</c> para concluir a auditoria de compensação da OS.
/// </summary>
public sealed record PagamentoEstornadoEvent : IntegrationEvent
{
    public required Guid PagamentoId { get; init; }
    public decimal? ValorEstornado { get; init; }
}

