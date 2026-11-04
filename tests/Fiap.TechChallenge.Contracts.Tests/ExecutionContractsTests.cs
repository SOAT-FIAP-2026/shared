using System.Text.Json;
using Fiap.TechChallenge.Contracts.Events;
using Fiap.TechChallenge.Contracts.ExecutionService;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class ExecutionContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ExecutionApiRoutes_devem_conter_formatos_corretos()
    {
        var id = Guid.NewGuid();
        var tarefaId = Guid.NewGuid();

        ExecutionApiRoutes.Base.Should().Be("api/v1/producao");
        ExecutionApiRoutes.FilaProducao.Should().Be("api/v1/producao/fila");
        ExecutionApiRoutes.DiagnosticosPendentes.Should().Be("api/v1/producao/diagnosticos/pendentes");
        string.Format(ExecutionApiRoutes.ObterPorId, id).Should().Be($"api/v1/producao/{id}");
        string.Format(ExecutionApiRoutes.ObterPorOrdemServicoId, id).Should().Be($"api/v1/producao/ordem-servico/{id}");
        string.Format(ExecutionApiRoutes.IniciarDiagnostico, id).Should().Be($"api/v1/producao/{id}/diagnostico/iniciar");
        string.Format(ExecutionApiRoutes.FinalizarDiagnostico, id).Should().Be($"api/v1/producao/{id}/diagnostico/finalizar");
        string.Format(ExecutionApiRoutes.IniciarTarefa, id, tarefaId).Should().Be($"api/v1/producao/{id}/tarefas/{tarefaId}/iniciar");
        string.Format(ExecutionApiRoutes.FinalizarTarefa, id, tarefaId).Should().Be($"api/v1/producao/{id}/tarefas/{tarefaId}/finalizar");
        string.Format(ExecutionApiRoutes.CancelarProducao, id).Should().Be($"api/v1/producao/{id}/cancelar");
        ExecutionApiRoutes.MetricasProdutividade.Should().Be("api/v1/producao/metricas/produtividade");
    }

    [Fact]
    public void HTTP_DTOs_de_Execution_devem_serializar_corretamente()
    {
        var servicoId = Guid.NewGuid();
        var pecaId = Guid.NewGuid();

        var finDiag = new FinalizarDiagnosticoRequest(
            "Laudo: Pastilhas de freio gastas",
            [new ServicoSugeridoRequest(servicoId, "Troca de pastilhas")],
            [new PecaSugeridaRequest(pecaId, 2, "Pastilha Bosch")]);

        finDiag.LaudoTecnico.Should().Be("Laudo: Pastilhas de freio gastas");
        finDiag.ServicosSugeridos.Should().HaveCount(1);
        finDiag.PecasSugeridas.Should().HaveCount(1);

        var finTarefa = new FinalizarTarefaRequest("Serviço concluído com teste de rodagem ok");
        finTarefa.Observacoes.Should().Be("Serviço concluído com teste de rodagem ok");

        var cancProd = new CancelarProducaoRequest("Cliente desistiu do reparo");
        cancProd.Motivo.Should().Be("Cliente desistiu do reparo");

        var json = JsonSerializer.Serialize(finDiag, JsonOptions);
        var copy = JsonSerializer.Deserialize<FinalizarDiagnosticoRequest>(json, JsonOptions);
        copy.Should().BeEquivalentTo(finDiag);
    }

    [Fact]
    public void Eventos_do_Execution_devem_funcionar_com_factories_Create_e_roundtrip_json()
    {
        var osId = Guid.NewGuid();
        var mecanicoId = Guid.NewGuid();
        var agora = DateTime.UtcNow;

        // DiagnosticoIniciadoEvent
        var diagIniciado = DiagnosticoIniciadoEvent.Create(osId, mecanicoId, "José Mecânico", agora);
        diagIniciado.OsId.Should().Be(osId);
        diagIniciado.IdMecanico.Should().Be(mecanicoId);
        diagIniciado.NomeMecanico.Should().Be("José Mecânico");
        diagIniciado.DataHora.Should().Be(agora);

        // DiagnosticoTecnicoConcluidoEvent com ServicoSugeridoInfo e PecaSugeridaInfo
        var servicoId = Guid.NewGuid();
        var pecaId = Guid.NewGuid();
        var diagConcluido1 = DiagnosticoTecnicoConcluidoEvent.Create(
            osId,
            "Laudo pericial",
            [new ServicoSugeridoInfo(servicoId, "Sangria de Freio")],
            [new PecaSugeridaInfo(pecaId, 1, "Fluido DOT4")],
            30);

        diagConcluido1.Laudo.Should().Be("Laudo pericial");
        diagConcluido1.DuracaoDiagnosticoMinutos.Should().Be(30);

        // DiagnosticoTecnicoConcluidoEvent com ServicoSugeridoItemEvent e PecaSugeridaItemEvent
        var diagConcluido2 = DiagnosticoTecnicoConcluidoEvent.Create(
            osId,
            "Laudo pericial",
            [new ServicoSugeridoItemEvent(servicoId, "Sangria de Freio")],
            [new PecaSugeridaItemEvent(pecaId, 1, "Fluido DOT4")],
            30);

        diagConcluido2.Servicos.First().IdServico.Should().Be(servicoId);
        diagConcluido2.Pecas.First().IdPeca.Should().Be(pecaId);

        // ProducaoIniciadaEvent
        var prodIniciada = ProducaoIniciadaEvent.Create(osId, agora);
        prodIniciada.OsId.Should().Be(osId);
        prodIniciada.DataHoraInicio.Should().Be(agora);

        // ProducaoFinalizadaEvent
        var prodFinalizada = ProducaoFinalizadaEvent.Create(osId, agora.AddHours(2), 120);
        prodFinalizada.OsId.Should().Be(osId);
        prodFinalizada.DuracaoTotalMinutos.Should().Be(120);

        // ProducaoCanceladaEvent
        var prodCancelada = ProducaoCanceladaEvent.Create(osId, "Peça incompatível", agora);
        prodCancelada.OsId.Should().Be(osId);
        prodCancelada.Motivo.Should().Be("Peça incompatível");
        prodCancelada.DataHoraCancelamento.Should().Be(agora);

        // Roundtrip JSON
        var json = JsonSerializer.Serialize(diagConcluido1, JsonOptions);
        var copy = JsonSerializer.Deserialize<DiagnosticoTecnicoConcluidoEvent>(json, JsonOptions);
        copy.Should().BeEquivalentTo(diagConcluido1);
    }

    [Fact]
    public void Compatibilidade_entre_modelos_de_Execution_deve_funcionar()
    {
        var id = Guid.NewGuid();

        // ServicoSugeridoItemEvent IS A ServicoSugeridoInfo
        ServicoSugeridoItemEvent sItem = new(id, "Troca de Vela");
        sItem.IdServico.Should().Be(id);
        sItem.Descricao.Should().Be("Troca de Vela");
        ServicoSugeridoInfo sInfo = sItem; // Upcast
        sInfo.IdServico.Should().Be(id);

        // PecaSugeridaItemEvent IS A PecaSugeridaInfo
        PecaSugeridaItemEvent pItem = new(id, 4, "Vela NGK");
        pItem.IdPeca.Should().Be(id);
        pItem.Quantidade.Should().Be(4);
        pItem.Descricao.Should().Be("Vela NGK");
        PecaSugeridaInfo pInfo = pItem; // Upcast
        pInfo.IdPeca.Should().Be(id);
        pInfo.Quantidade.Should().Be(4);
    }
}
