using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.TechChallenge.Contracts.BillingService;

/// <summary>
/// Lê texto ou número JSON como <see cref="string"/> (número vira o texto cru), como o <c>billing-service</c> faz
/// com os ids do webhook do Mercado Pago. Escreve sempre texto.
/// </summary>
internal sealed class TextoOuNumeroJsonConverter : JsonConverter<string?>
{
    public override bool HandleNull => true;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => System.Text.Encoding.UTF8.GetString(
                reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan),
            _ => throw new JsonException($"Esperado texto ou número, recebido {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
