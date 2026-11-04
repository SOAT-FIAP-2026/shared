using System.Text.Json;
using Fiap.TechChallenge.Contracts.Events;
using Fiap.TechChallenge.Contracts.OsService;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class OrdemServicoContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void OrdemServicoStatus_deve_conter_constantes_canonicas_e_aliases()
    {
        OrdemServicoStatus.RECEBIDA.Should().Be("RECEBIDA");
        OrdemServicoStatus.EM_DIAGNOSTICO.Should().Be("EM_DIAGNOSTICO");
        OrdemServicoStatus.AGUARDANDO_APROVACAO.Should().Be("AGUARDANDO_APROVACAO");
        OrdemServicoStatus.APROVADA.Should().Be("APROVADA");
        OrdemServicoStatus.REJEITADA.Should().Be("REJEITADA");
        OrdemServicoStatus.CANCELADA.Should().Be("CANCELADA");
        OrdemServicoStatus.EM_EXECUCAO.Should().Be("EM_EXECUCAO");
        OrdemServicoStatus.FINALIZADA.Should().Be("FINALIZADA");
        OrdemServicoStatus.ENTREGUE.Should().Be("ENTREGUE");

        OrdemServicoStatus.ORCAMENTO_APROVADO.Should().Be("ORCAMENTO_APROVADO");
        OrdemServicoStatus.ORCAMENTO_REPROVADO.Should().Be("ORCAMENTO_REPROVADO");
    }

    [Fact]
    public void OrdemServicoApiRoutes_devem_conter_formatos_corretos()
    {
        var id = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        OrdemServicoApiRoutes.Base.Should().Be("api/v1/ordens-servico");
        OrdemServicoApiRoutes.Criar.Should().Be("api/v1/ordens-servico");
        string.Format(OrdemServicoApiRoutes.ObterPorId, id).Should().Be($"api/v1/ordens-servico/{id}");
        string.Format(OrdemServicoApiRoutes.ObterProgresso, id).Should().Be($"api/v1/ordens-servico/{id}/progresso");
        string.Format(OrdemServicoApiRoutes.IncluirPecaInsumo, id).Should().Be($"api/v1/ordens-servico/{id}/pecas-insumos");
        string.Format(OrdemServicoApiRoutes.RemoverPecaInsumo, id, itemId).Should().Be($"api/v1/ordens-servico/{id}/pecas-insumos/{itemId}");
        string.Format(OrdemServicoApiRoutes.IncluirServico, id).Should().Be($"api/v1/ordens-servico/{id}/servicos");
        string.Format(OrdemServicoApiRoutes.RemoverServico, id, itemId).Should().Be($"api/v1/ordens-servico/{id}/servicos/{itemId}");
        string.Format(OrdemServicoApiRoutes.IniciarDiagnostico, id).Should().Be($"api/v1/ordens-servico/{id}/iniciar-diagnostico");
        string.Format(OrdemServicoApiRoutes.FinalizarDiagnostico, id).Should().Be($"api/v1/ordens-servico/{id}/finalizar-diagnostico");
        string.Format(OrdemServicoApiRoutes.AprovarOrcamento, id).Should().Be($"api/v1/ordens-servico/{id}/aprovar-orcamento");
        string.Format(OrdemServicoApiRoutes.IniciarServico, id, itemId).Should().Be($"api/v1/ordens-servico/{id}/servicos/{itemId}/iniciar");
        string.Format(OrdemServicoApiRoutes.FinalizarServico, id, itemId).Should().Be($"api/v1/ordens-servico/{id}/servicos/{itemId}/finalizar");
        string.Format(OrdemServicoApiRoutes.ConfirmarEntrega, id).Should().Be($"api/v1/ordens-servico/{id}/confirmar-entrega");
    }

    [Fact]
    public void HTTP_DTOs_de_OrdemServico_devem_serializar_corretamente()
    {
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var req = new OrdemServicoRequest(clienteId, veiculoId, "Barulho na suspensão dianteira");
        req.ClienteId.Should().Be(clienteId);
        req.VeiculoId.Should().Be(veiculoId);
        req.Observacao.Should().Be("Barulho na suspensão dianteira");

        var pecaReq = new OrdemServicoPecaInsumoRequest([Guid.NewGuid(), Guid.NewGuid()]);
        pecaReq.PecasInsumosIds.Should().HaveCount(2);

        var servicoReq = new OrdemServicoServicosRequest([Guid.NewGuid()]);
        servicoReq.ServicosIds.Should().HaveCount(1);

        var osId = Guid.NewGuid();
        var itemServico = new OrdemServicoItemServicoResponse(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), null, "Troca de amortecedor", 350m);
        var itemPeca = new OrdemServicoItemPecaInsumoResponse(Guid.NewGuid(), "Amortecedor Monroe", 420m);
        var response = new OrdemServicoResponse(
            osId,
            clienteId,
            veiculoId,
            Guid.NewGuid(),
            "Fernando Dias",
            "EM_EXECUCAO",
            "Veículo com revisão pendente",
            DateTime.UtcNow.AddDays(-1),
            null,
            770m,
            [itemServico],
            [itemPeca],
            ClienteNotificado: true);

        response.ValorTotal.Should().Be(770m);
        response.ClienteNotificado.Should().BeTrue();
        response.ItensServico.Should().HaveCount(1);
        response.ItensPecaInsumo.Should().HaveCount(1);

        var progressoServico = new OrdemServicoProgressoServicoResponse(
            itemServico.IdServico,
            "Troca de amortecedor",
            "EM_ANDAMENTO",
            120,
            60,
            DateTime.UtcNow.AddMinutes(-60),
            null);

        var progresso = new OrdemServicoProgressoResponse(
            osId,
            "EM_EXECUCAO",
            50,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(2),
            [progressoServico]);

        progresso.PercentualConcluido.Should().Be(50);
        progresso.Servicos.Should().HaveCount(1);

        // JSON roundtrip
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var copy = JsonSerializer.Deserialize<OrdemServicoResponse>(json, JsonOptions);
        copy.Should().BeEquivalentTo(response);
    }

    [Fact]
    public void CompensacaoExecutadaEvent_deve_funcionar_com_construtores_e_roundtrip_json()
    {
        var osId = Guid.NewGuid();
        var evt = CompensacaoExecutadaEvent.Create(osId, "comp-001", "EstornarEstoque", "techchallenge.ordem-servico");

        evt.OsId.Should().Be(osId);
        evt.CorrelationId.Should().Be(osId);
        evt.IdCompensacao.Should().Be("comp-001");
        evt.Tipo.Should().Be("EstornarEstoque");
        evt.Origem.Should().Be("techchallenge.ordem-servico");
        evt.TipoEvento.Should().Be("CompensacaoExecutadaEvent");

        var json = JsonSerializer.Serialize(evt, JsonOptions);
        var copy = JsonSerializer.Deserialize<CompensacaoExecutadaEvent>(json, JsonOptions);
        copy.Should().BeEquivalentTo(evt);
    }

    [Fact]
    public void OrdemServicoAbertaEvent_Create_deve_suportar_modelos_de_contrato()
    {
        var osId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();

        // Com ClienteInfo e VeiculoInfo
        var evt1 = OrdemServicoAbertaEvent.Create(
            osId,
            new ClienteInfo(clienteId, "Pedro", "11988887777", "pedro@email.com"),
            new VeiculoInfo("ABC1234", "Civic", 2020),
            "filial-sp",
            "Revisão 50k");

        evt1.Cliente.Nome.Should().Be("Pedro");
        evt1.Veiculo.Placa.Should().Be("ABC1234");
        evt1.FilialId.Should().Be("filial-sp");

        // Com ClienteContrato e VeiculoContrato
        var evt2 = OrdemServicoAbertaEvent.Create(
            osId,
            new ClienteContrato(clienteId, "Pedro", "11988887777"),
            new VeiculoContrato("ABC1234", "Civic", 2020),
            "filial-sp");

        evt2.Cliente.Nome.Should().Be("Pedro");
        evt2.Veiculo.Placa.Should().Be("ABC1234");
    }

    [Fact]
    public void OrdemServicoLiberadaParaExecucaoEvent_Create_deve_suportar_TarefaInfo_e_TarefaItemContrato()
    {
        var osId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        var evt1 = OrdemServicoLiberadaParaExecucaoEvent.Create(osId, [new TarefaInfo(servicoId, "Freios", 45)]);
        evt1.Tarefas.First().IdServico.Should().Be(servicoId);
        evt1.Tarefas.First().IdServicoOrigem.Should().Be(servicoId);

        var evt2 = OrdemServicoLiberadaParaExecucaoEvent.Create(osId, [new TarefaItemContrato(servicoId, "Freios", 45)]);
        evt2.Tarefas.First().IdServico.Should().Be(servicoId);
        evt2.Tarefas.First().IdServicoOrigem.Should().Be(servicoId);
    }

    [Fact]
    public void Compatibilidade_entre_modelos_de_OS_e_Contratos_deve_funcionar()
    {
        var id = Guid.NewGuid();

        // TarefaItemContrato IS A TarefaInfo
        TarefaItemContrato item = new(id, "Troca de Vela", 20);
        item.IdServicoOrigem.Should().Be(id);
        item.IdServico.Should().Be(id);
        TarefaInfo info = item; // Upcast
        info.IdServico.Should().Be(id);
        info.IdServicoOrigem.Should().Be(id);

        // ClienteContrato IS A ClienteInfo
        ClienteContrato cliContrato = new(id, "Ana", "11988881111");
        cliContrato.Nome.Should().Be("Ana");
        ClienteInfo cliInfo = cliContrato; // Upcast
        cliInfo.Nome.Should().Be("Ana");
        cliInfo.IdCliente.Should().Be(id);

        // VeiculoContrato IS A VeiculoInfo
        VeiculoContrato veiContrato = new("BRA2E19", "Gol", 2018);
        veiContrato.Placa.Should().Be("BRA2E19");
        VeiculoInfo veiInfo = veiContrato; // Upcast
        veiInfo.Placa.Should().Be("BRA2E19");
    }

    [Fact]
    public void ComandoCompensacaoContrato_deve_inicializar_corretamente()
    {
        var osId = Guid.NewGuid();
        var pecaId = Guid.NewGuid();
        var cmd = new ComandoCompensacaoContrato(
            "comp-123",
            TipoCompensacaoContrato.EstornarEstoque,
            osId,
            [pecaId],
            "Orçamento Reprovado",
            Guid.NewGuid());

        cmd.IdCompensacao.Should().Be("comp-123");
        cmd.Tipo.Should().Be(TipoCompensacaoContrato.EstornarEstoque);
        cmd.IdOrdemServico.Should().Be(osId);
        cmd.PecasInsumosIds.Should().Contain(pecaId);
        cmd.Motivo.Should().Be("Orçamento Reprovado");
    }
}
