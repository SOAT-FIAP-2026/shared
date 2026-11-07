using System.Text.Json;

namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Utilitário padronizado para extração de <c>X-Correlation-ID</c>, <c>OsId</c> e <c>tipoEvento</c>
/// a partir de mensagens AWS SQS / envelopes de notificação AWS SNS e payloads JSON.
/// </summary>
public static class SnsCorrelationHelper
{
    /// <summary>
    /// Extrai o <c>CorrelationId</c> de um corpo de mensagem (envelope SNS Notification ou JSON direto),
    /// considerando atributos externos (SQS/SNS), propriedades do JSON e fallback para o <c>OsId</c>.
    /// </summary>
    public static string ExtractCorrelationId(
        string? rawMessageBody,
        IReadOnlyDictionary<string, string?>? messageAttributes = null,
        string? fallbackId = null)
    {
        // 1. Atributos diretos da mensagem (SQS / SNS MessageAttributes)
        if (messageAttributes != null)
        {
            if (TryGetAttribute(messageAttributes, SnsMessageAttributes.CorrelationId, out var fromHeader) ||
                TryGetAttribute(messageAttributes, "correlationId", out fromHeader) ||
                TryGetAttribute(messageAttributes, "correlation_id", out fromHeader))
            {
                return fromHeader!;
            }
        }

        string? innerPayload = rawMessageBody;
        string? osIdFromEnvelope = null;

        // 2. Envelope SNS Notification ("Type": "Notification", "MessageAttributes": { ... }, "Message": "...")
        if (!string.IsNullOrWhiteSpace(rawMessageBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(rawMessageBody);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("MessageAttributes", out var msgAttrs) && msgAttrs.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetSnsEnvelopeAttribute(msgAttrs, SnsMessageAttributes.CorrelationId, out var snsCorr) ||
                            TryGetSnsEnvelopeAttribute(msgAttrs, "correlationId", out snsCorr) ||
                            TryGetSnsEnvelopeAttribute(msgAttrs, "correlation_id", out snsCorr))
                        {
                            return snsCorr!;
                        }

                        if (TryGetSnsEnvelopeAttribute(msgAttrs, SnsMessageAttributes.IdOrdemServico, out var snsOsId))
                        {
                            osIdFromEnvelope = snsOsId;
                        }
                    }

                    if (root.TryGetProperty("Message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    {
                        innerPayload = msgProp.GetString();
                    }
                }
            }
            catch
            {
                // Ignora erro de parse caso o body não seja JSON válido
            }
        }

        // 3. Payload JSON interno do evento/comando
        if (!string.IsNullOrWhiteSpace(innerPayload))
        {
            try
            {
                using var innerDoc = JsonDocument.Parse(innerPayload);
                var root = innerDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetJsonString(root, out var jsonCorr, "xCorrelationId", "XCorrelationId", "X-Correlation-ID", "correlationId", "CorrelationId"))
                    {
                        return jsonCorr!;
                    }

                    if (TryGetJsonString(root, out var jsonOsId, "osId", "OsId", "idOrdemServico", "IdOrdemServico"))
                    {
                        return jsonOsId!;
                    }
                }
            }
            catch
            {
                // Ignora erro de parse
            }
        }

        // 4. Fallback para atributo idOrdemServico da mensagem ou fallbackId
        if (!string.IsNullOrWhiteSpace(osIdFromEnvelope))
            return osIdFromEnvelope!;

        if (messageAttributes != null && TryGetAttribute(messageAttributes, SnsMessageAttributes.IdOrdemServico, out var attrOsId))
            return attrOsId!;

        if (!string.IsNullOrWhiteSpace(fallbackId))
            return fallbackId.Trim();

        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Extrai o <c>OsId</c> (identificador da Ordem de Serviço) de um envelope SNS ou payload JSON, se disponível.
    /// </summary>
    public static string? ExtractOsId(
        string? rawMessageBody,
        IReadOnlyDictionary<string, string?>? messageAttributes = null)
    {
        if (messageAttributes != null && TryGetAttribute(messageAttributes, SnsMessageAttributes.IdOrdemServico, out var attrOsId))
            return attrOsId;

        string? innerPayload = rawMessageBody;

        if (!string.IsNullOrWhiteSpace(rawMessageBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(rawMessageBody);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("MessageAttributes", out var msgAttrs) &&
                        msgAttrs.ValueKind == JsonValueKind.Object &&
                        TryGetSnsEnvelopeAttribute(msgAttrs, SnsMessageAttributes.IdOrdemServico, out var snsOsId))
                    {
                        return snsOsId;
                    }

                    if (root.TryGetProperty("Message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    {
                        innerPayload = msgProp.GetString();
                    }
                }
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(innerPayload))
        {
            try
            {
                using var innerDoc = JsonDocument.Parse(innerPayload);
                var root = innerDoc.RootElement;
                if (root.ValueKind == JsonValueKind.Object &&
                    TryGetJsonString(root, out var jsonOsId, "osId", "OsId", "idOrdemServico", "IdOrdemServico"))
                {
                    return jsonOsId;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static bool TryGetAttribute(IReadOnlyDictionary<string, string?> attributes, string key, out string? value)
    {
        foreach (var kvp in attributes)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kvp.Value))
            {
                value = kvp.Value.Trim();
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetSnsEnvelopeAttribute(JsonElement messageAttributes, string key, out string? value)
    {
        foreach (var prop in messageAttributes.EnumerateObject())
        {
            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Object)
            {
                if ((prop.Value.TryGetProperty("Value", out var valProp) || prop.Value.TryGetProperty("value", out valProp)) &&
                    valProp.ValueKind == JsonValueKind.String)
                {
                    var str = valProp.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        value = str.Trim();
                        return true;
                    }
                }
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetJsonString(JsonElement obj, out string? value, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (obj.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && str != Guid.Empty.ToString())
                {
                    value = str.Trim();
                    return true;
                }
            }
        }

        value = null;
        return false;
    }
}

