using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Configurações de <see cref="JsonSerializer"/> que os serviços usam hoje para colocar contratos no fio.
/// </summary>
/// <remarks>
/// Catálogo de referência: documenta o formato real, não impõe um padrão. As instâncias são somente leitura.
/// </remarks>
public static class WireJsonOptions
{
    /// <summary>
    /// Eventos publicados pelo <c>os-microservice</c> (outbox e publicação direta):
    /// camelCase, nulos omitidos e enums como texto.
    /// Cópia de <c>os-microservice/src/OrdemServico.Lambda/Configuration/JsonDefaults.cs</c>.
    /// </summary>
    public static JsonSerializerOptions OsEventos { get; } = Congelar(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    });

    /// <summary>
    /// Padrão do <c>System.Text.Json</c> (PascalCase, nulos escritos, enums como número).
    /// Usado pelos eventos do <c>billing-service</c> e do <c>execution-service</c> e pela resposta do <c>lambda-auth</c>.
    /// </summary>
    public static JsonSerializerOptions PadraoDotNet { get; } = Congelar(new JsonSerializerOptions());

    /// <summary>
    /// Padrão HTTP do ASP.NET Core (camelCase, leitura sem distinção de maiúsculas).
    /// Usado pelas APIs de todos os serviços.
    /// </summary>
    public static JsonSerializerOptions Http { get; } = Congelar(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static JsonSerializerOptions Congelar(JsonSerializerOptions opcoes)
    {
        // populateMissingResolver usa o resolver padrão já congelado: um DefaultJsonTypeInfoResolver novo
        // deixaria a lista Modifiers aberta até o primeiro uso.
        opcoes.MakeReadOnly(populateMissingResolver: true);
        return opcoes;
    }
}
