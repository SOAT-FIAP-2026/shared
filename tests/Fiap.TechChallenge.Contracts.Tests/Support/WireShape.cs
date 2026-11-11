using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Fiap.TechChallenge.Contracts.Tests.Support;

/// <summary>
/// Descreve a forma de um tipo no fio (nomes de propriedade, tipo JSON e nulabilidade)
/// a partir da visão do <see cref="JsonSerializer"/> (<see cref="JsonTypeInfo"/>).
/// </summary>
/// <remarks>
/// Também é compilado pela ferramenta de captura de fixtures (fora do repositório). Não depender de nada além de System.Text.Json.
/// </remarks>
public static class WireShape
{
    private static readonly JsonSerializerOptions OpcoesSaida = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly HashSet<Type> TiposTexto =
    [
        typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(char),
        typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly), typeof(Uri)
    ];

    private static readonly HashSet<Type> TiposNumero =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
    ];

    public static JsonObject Extrair(Type tipo, JsonSerializerOptions opcoes) =>
        ExtrairObjeto(tipo, opcoes, []);

    public static string ParaJson(JsonNode forma) => forma.ToJsonString(OpcoesSaida);

    private static JsonObject ExtrairObjeto(Type tipo, JsonSerializerOptions opcoes, HashSet<Type> pilha)
    {
        var info = opcoes.GetTypeInfo(tipo);
        var contexto = new NullabilityInfoContext();
        var resultado = new JsonObject();

        pilha.Add(tipo);
        foreach (var propriedade in info.Properties.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var nulavel = EhReferenciaNulavel(propriedade, contexto);
            var textoEnum = UsaEnumComoTexto(propriedade, opcoes);
            resultado[propriedade.Name] = Forma(propriedade.PropertyType, nulavel, textoEnum, opcoes, pilha);
        }
        pilha.Remove(tipo);

        return resultado;
    }

    private static JsonNode Forma(Type tipo, bool nulavel, bool textoEnum, JsonSerializerOptions opcoes, HashSet<Type> pilha)
    {
        var subjacente = Nullable.GetUnderlyingType(tipo);
        if (subjacente is not null)
        {
            tipo = subjacente;
            nulavel = true;
        }

        var sufixo = nulavel ? "?" : string.Empty;

        if (TiposTexto.Contains(tipo)) return JsonValue.Create("string" + sufixo);
        if (TiposNumero.Contains(tipo)) return JsonValue.Create("number" + sufixo);
        if (tipo == typeof(bool)) return JsonValue.Create("boolean" + sufixo);
        if (tipo.IsEnum) return JsonValue.Create((textoEnum ? "enum:string" : "enum:number") + sufixo);
        if (tipo == typeof(object) || tipo == typeof(JsonElement) || typeof(JsonNode).IsAssignableFrom(tipo))
            return JsonValue.Create("any" + sufixo);
        if (ImplementaGenerico(tipo, typeof(IDictionary<,>)) || ImplementaGenerico(tipo, typeof(IReadOnlyDictionary<,>)))
            return JsonValue.Create("map" + sufixo);

        JsonNode forma;
        var elemento = TipoElemento(tipo);
        if (elemento is not null)
        {
            forma = new JsonArray(Forma(elemento, false, UsaEnumComoTexto(null, opcoes), opcoes, pilha));
        }
        else if (pilha.Contains(tipo))
        {
            return JsonValue.Create("ref:" + tipo.Name + sufixo);
        }
        else
        {
            forma = ExtrairObjeto(tipo, opcoes, pilha);
        }

        return nulavel
            ? new JsonObject { ["$nullable"] = true, ["$forma"] = forma }
            : forma;
    }

    private static bool EhReferenciaNulavel(JsonPropertyInfo propriedade, NullabilityInfoContext contexto)
    {
        if (propriedade.PropertyType.IsValueType) return false;

        var estado = propriedade.AttributeProvider switch
        {
            PropertyInfo p => contexto.Create(p).ReadState,
            FieldInfo f => contexto.Create(f).ReadState,
            _ => NullabilityState.Unknown
        };

        return estado == NullabilityState.Nullable;
    }

    private static bool UsaEnumComoTexto(JsonPropertyInfo? propriedade, JsonSerializerOptions opcoes)
    {
        static bool EhConversorTexto(object? conversor) =>
            conversor is not null && conversor.GetType().Name.StartsWith("JsonStringEnumConverter", StringComparison.Ordinal);

        return EhConversorTexto(propriedade?.CustomConverter) || opcoes.Converters.Any(EhConversorTexto);
    }

    private static Type? TipoElemento(Type tipo)
    {
        if (tipo == typeof(string)) return null;
        if (tipo.IsArray) return tipo.GetElementType();

        var enumeravel = tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? tipo
            : tipo.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumeravel?.GetGenericArguments()[0];
    }

    private static bool ImplementaGenerico(Type tipo, Type definicao) =>
        (tipo.IsGenericType && tipo.GetGenericTypeDefinition() == definicao)
        || tipo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == definicao);
}
