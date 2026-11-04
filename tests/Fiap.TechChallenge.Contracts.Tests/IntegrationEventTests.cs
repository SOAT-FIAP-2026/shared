using System.Text.Json;
using Fiap.TechChallenge.Contracts.AuthService;
using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.Events;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class IntegrationEventTests
{
    private const string EventsNamespace = "Fiap.TechChallenge.Contracts.Events";

    private static readonly Type[] EventTypes = typeof(IntegrationEvent).Assembly
        .GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(IntegrationEvent)))
        .ToArray();

    [Fact]
    public void Catalogo_deve_conter_todos_os_eventos_dos_servicos()
    {
        EventTypes.Select(t => t.Name).Should().BeEquivalentTo(
            // os-microservice
            "OrdemServicoAbertaEvent",
            "DiagnosticoRejeitadoEvent",
            "DiagnosticoFinalizadoEvent",
            "OrdemServicoLiberadaParaExecucaoEvent",
            "OrdemServicoCanceladaEvent",
            "OrcamentoReprovadoEvent",
            "PecasInsumoIncluidasEvent",
            "StatusOrdemServicoAlteradoEvent",
            "CompensacaoExecutadaEvent",
            // execution-service
            "DiagnosticoIniciadoEvent",
            "DiagnosticoTecnicoConcluidoEvent",
            "ProducaoIniciadaEvent",
            "ProducaoFinalizadaEvent",
            "ProducaoCanceladaEvent",
            // billing-service
            "OrcamentoGeradoEvent",
            "PagamentoConfirmadoEvent",
            "PagamentoCanceladoCompensacaoEvent",
            "PagamentoRecusadoEvent",
            "PagamentoEstornadoEvent",
            // fase1-tech-challenge (Core / Estoque)
            "ReservaEstoqueFalhouEvent");
    }

    [Fact]
    public void Eventos_devem_estar_no_namespace_compartilhado()
    {
        EventTypes.Should().OnlyContain(t => t.Namespace == EventsNamespace);
    }

    [Fact]
    public void Eventos_devem_ser_sealed_e_terminar_com_Event()
    {
        EventTypes.Should().OnlyContain(t => t.IsSealed && t.Name.EndsWith("Event"));
    }

    [Fact]
    public void Envelope_deve_preencher_metadados_tipoEvento_e_correlacionar_pelo_osId()
    {
        var osId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var evt = new ProducaoCanceladaEvent { OsId = osId, Motivo = "Motor com dano estrutural" };

        evt.EventId.Should().NotBeEmpty();
        evt.CorrelationId.Should().Be(osId);
        evt.TipoEvento.Should().Be("ProducaoCanceladaEvent");
        evt.Version.Should().Be(1);
        evt.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        evt.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Eventos_distintos_devem_ter_EventId_distintos()
    {
        var osId = Guid.NewGuid();
        var a = new OrdemServicoCanceladaEvent { OsId = osId, Motivo = "x" };
        var b = new OrdemServicoCanceladaEvent { OsId = osId, Motivo = "x" };

        a.EventId.Should().NotBe(b.EventId);
    }

    [Fact]
    public void OrdemServicoAbertaEvent_deve_sobreviver_ao_roundtrip_json()
    {
        var original = new OrdemServicoAbertaEvent
        {
            OsId = Guid.NewGuid(),
            FilialId = "sp-matriz",
            Observacao = "Ruído ao frear",
            Cliente = new ClienteInfo(Guid.NewGuid(), "João da Silva", "+5511999998888", "joao@email.com"),
            Veiculo = new VeiculoInfo("ABC1D23", "Honda Civic", 2022)
        };

        var copy = RoundTrip(original);

        copy.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void DiagnosticoTecnicoConcluidoEvent_deve_sobreviver_ao_roundtrip_json()
    {
        var original = new DiagnosticoTecnicoConcluidoEvent
        {
            OsId = Guid.NewGuid(),
            Laudo = "Vazamento na junta do cárter.",
            Servicos = [new ServicoSugeridoInfo(Guid.NewGuid(), "Troca de Junta do Cárter")],
            Pecas = [new PecaSugeridaInfo(Guid.NewGuid(), 4, "Óleo 5W30")],
            DuracaoDiagnosticoMinutos = 45
        };

        var copy = RoundTrip(original);

        copy.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void OrdemServicoLiberadaParaExecucaoEvent_deve_sobreviver_ao_roundtrip_json()
    {
        var original = new OrdemServicoLiberadaParaExecucaoEvent
        {
            OsId = Guid.NewGuid(),
            Tarefas = [new TarefaInfo(Guid.NewGuid(), "Troca de Óleo", 30)]
        };

        var copy = RoundTrip(original);

        copy.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Constantes_sns_e_jwt_devem_estar_definidas()
    {
        SnsMessageAttributes.TipoEvento.Should().Be("tipoEvento");
        SnsMessageAttributes.Origens.OsService.Should().Be("techchallenge.ordem-servico");
        JwtAuthConstants.Roles.Mecanico.Should().Be("Mecanico");
    }

    [Fact]
    public void Desserializacao_sem_OsId_deve_falhar_pois_o_campo_e_obrigatorio()
    {
        const string json = """{ "motivo": "x" }""";

        var act = () => JsonSerializer.Deserialize<OrdemServicoCanceladaEvent>(json, JsonOptions);

        act.Should().Throw<JsonException>();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static T RoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }
}

