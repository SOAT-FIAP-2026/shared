namespace Fiap.TechChallenge.Contracts.BillingService;

/// <summary>Métodos de pagamento suportados na integração com o Mercado Pago.</summary>
public enum MetodoPagamentoContrato
{
    Pix,
    CartaoCredito,
    Boleto
}

/// <summary>Status do pagamento rastreado pelo <c>billing-service</c>.</summary>
public enum StatusPagamentoContrato
{
    Pendente,
    Aprovado,
    Recusado,
    Expirado,
    Estornado
}

/// <summary>Payload de notificação de Webhook do Mercado Pago recebido pelo <c>billing-service</c>.</summary>
public sealed record MercadoPagoWebhookRequest(
    string Action,
    string Type,
    MercadoPagoWebhookData Data);

public sealed record MercadoPagoWebhookData(string Id);

