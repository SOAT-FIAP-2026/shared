using System.Text.Json.Serialization;

namespace Fiap.TechChallenge.Contracts.BillingService;

/// <summary>Métodos de pagamento na integração com o Mercado Pago.</summary>
/// <remarks>
/// Não trafega hoje: o <c>billing-service</c> guarda <c>MeioPagamento</c> como texto (sempre <c>"boleto"</c>)
/// e envia <c>payment_method_id = "bolbradesco"</c> ao Mercado Pago.
/// </remarks>
public enum MetodoPagamentoContrato
{
    Pix,
    CartaoCredito,
    Boleto
}

/// <summary>
/// Status do pagamento no <c>billing-service</c>. Espelho de <c>PagamentoStatus</c>
/// (<c>billing-service/src/Billing.Service.Domain/Entities/Pagamento.cs</c>), com os mesmos valores numéricos.
/// </summary>
/// <remarks>
/// No fio aparece só como texto minúsculo em <see cref="BoletoResponse.Status"/>
/// (<c>pendente</c>, <c>aprovado</c>, <c>rejeitado</c>, <c>cancelado</c>, <c>expirado</c>).
/// O status <c>refunded</c> do Mercado Pago vira <see cref="Cancelado"/>.
/// </remarks>
public enum StatusPagamentoContrato
{
    Pendente = 0,
    Aprovado = 1,
    Rejeitado = 2,
    Cancelado = 3,
    Expirado = 4
}

/// <summary>
/// Notificação de webhook do Mercado Pago recebida em <see cref="BillingApiRoutes.WebhookMercadoPago"/>.
/// </summary>
/// <remarks>
/// O <c>billing-service</c> lê o corpo como <c>JsonElement</c>. O id do pagamento é procurado, nesta ordem, em:
/// <c>data.id</c>, <c>data</c> como valor simples (texto ou número), <c>id</c> na raiz, query <c>data.id</c>,
/// query <c>id</c>. A forma com <c>data</c> como valor simples não é coberta por este record.
/// <see cref="Status"/> (raiz, ou <see cref="MercadoPagoWebhookData.Status"/>) só alimenta o modo mock.
/// <see cref="Action"/> e <see cref="Type"/> são ignorados. Sem id → 400 com texto; assinatura inválida → 401 sem
/// corpo; demais casos → 200 sem corpo.
/// </remarks>
public sealed record MercadoPagoWebhookRequest(
    string? Action,
    string? Type,
    MercadoPagoWebhookData? Data,
    [property: JsonConverter(typeof(TextoOuNumeroJsonConverter))] string? Id = null,
    [property: JsonConverter(typeof(TextoOuNumeroJsonConverter))] string? Status = null);

/// <summary>Bloco <c>data</c> do webhook. <see cref="Id"/> aceita texto ou número.</summary>
public sealed record MercadoPagoWebhookData(
    [property: JsonConverter(typeof(TextoOuNumeroJsonConverter))] string? Id,
    [property: JsonConverter(typeof(TextoOuNumeroJsonConverter))] string? Status = null);

/// <summary>Cabeçalhos, chaves e parâmetros que o <c>billing-service</c> lê do webhook do Mercado Pago.</summary>
/// <remarks>
/// A assinatura só é validada quando <c>MercadoPago:WebhookSecret</c> está configurado: HMAC-SHA256 em hex
/// minúsculo do <see cref="FormatoManifesto"/>, comparado em tempo constante com <see cref="ChaveAssinatura"/>.
/// O cabeçalho <see cref="HeaderAssinatura"/> é separado por <c>,</c> ou <c>;</c> em pares chave=valor.
/// </remarks>
public static class MercadoPagoWebhookParametros
{
    public const string HeaderAssinatura = "x-signature";
    public const string HeaderRequestId = "x-request-id";
    public const string ChaveTimestamp = "ts";
    public const string ChaveAssinatura = "v1";
    public const string QueryDataId = "data.id";
    public const string QueryId = "id";

    /// <summary>Manifesto assinado: <c>{0}</c> = id do pagamento, <c>{1}</c> = x-request-id, <c>{2}</c> = ts.</summary>
    public const string FormatoManifesto = "id:{0};request-id:{1};ts:{2};";
}
