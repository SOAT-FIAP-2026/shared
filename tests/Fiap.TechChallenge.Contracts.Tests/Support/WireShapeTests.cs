using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fiap.TechChallenge.Contracts.Common;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Support;

public class WireShapeTests
{
    public sealed record ExemploHttp(Guid Id, string Nome);

    public sealed record ExemploNulavel(string? A, int? B, string C);

    public sealed record ExemploJsonName([property: JsonPropertyName("access_token")] string Token);

    public enum ExemploEnum { Primeiro, Segundo }

    public sealed record ExemploComEnum(ExemploEnum Tipo);

    public sealed record ExemploLista(IReadOnlyList<ExemploHttp> Itens, IReadOnlyCollection<Guid>? Ids);

    public sealed record ExemploEvento : IntegrationEvent;

    private static IEnumerable<string> Chaves(JsonObject forma) => forma.Select(p => p.Key);

    [Fact]
    public void WireShape_extrai_nomes_camelCase_com_opcoes_Http()
    {
        var forma = WireShape.Extrair(typeof(ExemploHttp), WireJsonOptions.Http);

        Chaves(forma).Should().Equal("id", "nome");
        forma["id"]!.GetValue<string>().Should().Be("string");
    }

    [Fact]
    public void WireShape_marca_nulabilidade()
    {
        var forma = WireShape.Extrair(typeof(ExemploNulavel), WireJsonOptions.Http);

        forma["a"]!.GetValue<string>().Should().Be("string?");
        forma["b"]!.GetValue<string>().Should().Be("number?");
        forma["c"]!.GetValue<string>().Should().Be("string");
    }

    [Fact]
    public void WireShape_respeita_JsonPropertyName()
    {
        var forma = WireShape.Extrair(typeof(ExemploJsonName), WireJsonOptions.PadraoDotNet);

        Chaves(forma).Should().Equal("access_token");
    }

    [Fact]
    public void WireShape_enum_texto_vs_numero()
    {
        WireShape.Extrair(typeof(ExemploComEnum), WireJsonOptions.OsEventos)["tipo"]!
            .GetValue<string>().Should().Be("enum:string");
        WireShape.Extrair(typeof(ExemploComEnum), WireJsonOptions.PadraoDotNet)["Tipo"]!
            .GetValue<string>().Should().Be("enum:number");
    }

    [Fact]
    public void WireShape_descreve_listas_pelo_tipo_do_elemento()
    {
        var forma = WireShape.Extrair(typeof(ExemploLista), WireJsonOptions.Http);

        var itens = forma["itens"]!.AsArray();
        itens.Should().HaveCount(1);
        Chaves(itens[0]!.AsObject()).Should().Equal("id", "nome");

        var ids = forma["ids"]!.AsObject();
        ids["$nullable"]!.GetValue<bool>().Should().BeTrue();
        ids["$forma"]!.AsArray()[0]!.GetValue<string>().Should().Be("string");
    }

    [Fact]
    public void WireShape_inclui_propriedade_calculada()
    {
        var forma = WireShape.Extrair(typeof(ExemploEvento), WireJsonOptions.Http);

        Chaves(forma).Should().Contain(["tipoEvento", "correlationId", "osId", "eventId"]);
    }

    [Fact]
    public void WireJsonOptions_sao_read_only()
    {
        WireJsonOptions.OsEventos.IsReadOnly.Should().BeTrue();
        WireJsonOptions.PadraoDotNet.IsReadOnly.Should().BeTrue();
        WireJsonOptions.Http.IsReadOnly.Should().BeTrue();
    }

    public static TheoryData<string> NomesOpcoes => new() { "OsEventos", "PadraoDotNet", "Http" };

    [Theory]
    [MemberData(nameof(NomesOpcoes))]
    public void WireJsonOptions_resolver_nao_aceita_modificadores(string nome)
    {
        var opcoes = (JsonSerializerOptions)typeof(WireJsonOptions).GetProperty(nome)!.GetValue(null)!;
        var resolver = opcoes.TypeInfoResolver.Should().BeAssignableTo<DefaultJsonTypeInfoResolver>().Subject;

        var adicionar = () => resolver.Modifiers.Add(_ => { });

        adicionar.Should().Throw<InvalidOperationException>();
    }
}
