using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Support;

/// <summary>
/// Lê as fixtures capturadas dos tipos reais dos serviços (<c>Fixtures/wire/*.json</c>)
/// e compara com a forma dos tipos do <c>shared</c>.
/// </summary>
public static class WireFixture
{
    private static readonly string Diretorio = Path.Combine(AppContext.BaseDirectory, "Fixtures", "wire");

    public static JsonNode Ler(string nome) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(Diretorio, nome + ".json")))!;

    public static JsonNode Forma(string nome) => Ler(nome)["forma"]!;

    public static void DeveEspelhar<T>(string nome, JsonSerializerOptions opcoes) =>
        DeveEspelhar(typeof(T), nome, opcoes);

    public static void DeveEspelhar(Type tipo, string nome, JsonSerializerOptions opcoes)
    {
        var esperado = WireShape.ParaJson(Forma(nome));
        var atual = WireShape.ParaJson(WireShape.Extrair(tipo, opcoes));

        atual.Should().Be(esperado, $"{tipo.FullName} deve espelhar a fixture '{nome}'");
    }
}
