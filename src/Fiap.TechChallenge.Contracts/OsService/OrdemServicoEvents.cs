using System.Diagnostics.CodeAnalysis;

namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando uma nova Ordem de Serviço é aberta (status <c>RECEBIDA</c>).
/// Consumido pelo <c>execution-service</c> para criar a <c>OrdemProducao</c> em <c>AGUARDANDO_DIAGNOSTICO</c>.
/// </summary>
public sealed record OrdemServicoAbertaEvent : IntegrationEvent
{
    public required ClienteInfo Cliente { get; init; }
    public required VeiculoInfo Veiculo { get; init; }
    public string? FilialId { get; init; }
    public string? Observacao { get; init; }

    public OrdemServicoAbertaEvent() { }

    [SetsRequiredMembers]
    public OrdemServicoAbertaEvent(
        Guid osId,
        ClienteInfo cliente,
        VeiculoInfo veiculo,
        string? filialId = null,
        string? observacao = null)
    {
        OsId = osId;
        Cliente = cliente;
        Veiculo = veiculo;
        FilialId = filialId;
        Observacao = observacao;
    }

    public static OrdemServicoAbertaEvent Create(
        Guid osId,
        ClienteInfo cliente,
        VeiculoInfo veiculo,
        string? filialId = null,
        string? observacao = null) =>
        new(osId, cliente, veiculo, filialId, observacao);
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando itens do diagnóstico técnico falham na validação de catálogo
/// ou na Saga de reserva de estoque junto ao <c>fase1-tech-challenge</c> (Core).
/// Consumido pelo <c>execution-service</c> para retornar a ordem ao status <c>EM_DIAGNOSTICO</c>.
/// </summary>
/// <remarks>Não trafega hoje: nenhum serviço publica este evento (o execution-service consome).</remarks>
public sealed record DiagnosticoRejeitadoEvent : IntegrationEvent
{
    public required string Motivo { get; init; }

    public DiagnosticoRejeitadoEvent() { }

    [SetsRequiredMembers]
    public DiagnosticoRejeitadoEvent(Guid osId, string motivo)
    {
        OsId = osId;
        Motivo = motivo;
    }

    public static DiagnosticoRejeitadoEvent Create(Guid osId, string motivo) =>
        new(osId, motivo);
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> após validar os itens do diagnóstico no catálogo do Core,
/// reservar as peças no estoque e calcular o valor da OS (status <c>AGUARDANDO_APROVACAO</c>).
/// Consumido pelo <c>billing-service</c> para gerar o orçamento e disponibilizar a cobrança Mercado Pago.
/// </summary>
/// <remarks>Não trafega hoje: nenhum serviço publica este evento (o billing-service consome). O finalizar-diagnóstico do OS emite só <c>StatusOrdemServicoAlterado</c>; o billing gera orçamento a partir de <c>DiagnosticoTecnicoConcluidoEvent</c> com preços fixos.</remarks>
public sealed record DiagnosticoFinalizadoEvent : IntegrationEvent
{
    public required IReadOnlyList<ItemOrcamentoInfo> Itens { get; init; }
    public required decimal Total { get; init; }
    public required ClienteInfo Cliente { get; init; }

    public DiagnosticoFinalizadoEvent() { }

    [SetsRequiredMembers]
    public DiagnosticoFinalizadoEvent(
        Guid osId,
        IReadOnlyList<ItemOrcamentoInfo> itens,
        decimal total,
        ClienteInfo cliente)
    {
        OsId = osId;
        Itens = itens;
        Total = total;
        Cliente = cliente;
    }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> após o cliente aprovar o orçamento e o <c>billing-service</c>
/// confirmar o pagamento (status <c>ORCAMENTO_APROVADO</c>).
/// Consumido pelo <c>execution-service</c> para inserir a ordem na fila FIFO (<c>NA_FILA</c>).
/// </summary>
public sealed record OrdemServicoLiberadaParaExecucaoEvent : IntegrationEvent
{
    public required IReadOnlyList<TarefaInfo> Tarefas { get; init; }

    public OrdemServicoLiberadaParaExecucaoEvent() { }

    [SetsRequiredMembers]
    public OrdemServicoLiberadaParaExecucaoEvent(Guid osId, IReadOnlyList<TarefaInfo> tarefas)
    {
        OsId = osId;
        Tarefas = tarefas;
    }

    public static OrdemServicoLiberadaParaExecucaoEvent Create(Guid osId, IReadOnlyList<TarefaInfo> tarefas) =>
        new(osId, tarefas);
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando a OS é cancelada (Compensação A ou falha de pagamento).
/// Consumido pelo <c>execution-service</c> (cancela produção/interrompe tarefas) e pela função de compensação
/// do <c>os-microservice</c> (estorna peças no estoque do Core).
/// </summary>
public sealed record OrdemServicoCanceladaEvent : IntegrationEvent
{
    public required string Motivo { get; init; }
    public IReadOnlyList<Guid> PecasInsumosIds { get; init; } = [];

    public OrdemServicoCanceladaEvent() { }

    [SetsRequiredMembers]
    public OrdemServicoCanceladaEvent(Guid osId, string motivo, IReadOnlyList<Guid>? pecasInsumosIds = null)
    {
        OsId = osId;
        Motivo = motivo;
        PecasInsumosIds = pecasInsumosIds ?? [];
    }

    public static OrdemServicoCanceladaEvent Create(Guid osId, string motivo, IReadOnlyList<Guid>? pecasInsumosIds = null) =>
        new(osId, motivo, pecasInsumosIds);
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando o cliente reprova o orçamento.
/// Dispara o estorno das peças reservadas no estoque do Core (<c>fase1-tech-challenge</c>).
/// </summary>
/// <remarks>Não trafega hoje: o os-microservice publica <see cref="Fiap.TechChallenge.Contracts.OsService.DomainEvents.OrcamentoReprovado"/> via outbox.</remarks>
public sealed record OrcamentoReprovadoEvent : IntegrationEvent
{
    public required IReadOnlyList<Guid> PecasInsumosIds { get; init; }

    public OrcamentoReprovadoEvent() { }

    [SetsRequiredMembers]
    public OrcamentoReprovadoEvent(Guid osId, IReadOnlyList<Guid> pecasInsumosIds)
    {
        OsId = osId;
        PecasInsumosIds = pecasInsumosIds;
    }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> ao concluir a Saga de reserva de peças/insumos no estoque.
/// </summary>
/// <remarks>Não trafega hoje: o os-microservice publica <see cref="Fiap.TechChallenge.Contracts.OsService.DomainEvents.PecasInsumoIncluidas"/> via outbox.</remarks>
public sealed record PecasInsumoIncluidasEvent : IntegrationEvent
{
    public required Guid IdSaga { get; init; }
    public required IReadOnlyList<Guid> PecasInsumosIds { get; init; }

    public PecasInsumoIncluidasEvent() { }

    [SetsRequiredMembers]
    public PecasInsumoIncluidasEvent(Guid osId, Guid idSaga, IReadOnlyList<Guid> pecasInsumosIds)
    {
        OsId = osId;
        IdSaga = idSaga;
        PecasInsumosIds = pecasInsumosIds;
    }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> a cada mudança de status da Ordem de Serviço.
/// </summary>
/// <remarks>Não trafega hoje: o os-microservice publica <see cref="Fiap.TechChallenge.Contracts.OsService.DomainEvents.StatusOrdemServicoAlterado"/> via outbox.</remarks>
public sealed record StatusOrdemServicoAlteradoEvent : IntegrationEvent
{
    public required string StatusAnterior { get; init; }
    public required string StatusAtual { get; init; }

    public StatusOrdemServicoAlteradoEvent() { }

    [SetsRequiredMembers]
    public StatusOrdemServicoAlteradoEvent(Guid osId, string statusAnterior, string statusAtual)
    {
        OsId = osId;
        StatusAnterior = statusAnterior;
        StatusAtual = statusAtual;
    }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> ao concluir uma etapa da Saga de compensação.
/// </summary>
/// <remarks>Não trafega hoje: o os-microservice publica <see cref="Fiap.TechChallenge.Contracts.OsService.DomainEvents.CompensacaoExecutada"/> via outbox.</remarks>
public sealed record CompensacaoExecutadaEvent : IntegrationEvent
{
    public required string IdCompensacao { get; init; }
    public required string Tipo { get; init; }
    public string? Origem { get; init; }

    public CompensacaoExecutadaEvent() { }

    [SetsRequiredMembers]
    public CompensacaoExecutadaEvent(Guid osId, string idCompensacao, string tipo, string? origem = null)
    {
        OsId = osId;
        IdCompensacao = idCompensacao;
        Tipo = tipo;
        Origem = origem;
    }

    public static CompensacaoExecutadaEvent Create(Guid osId, string idCompensacao, string tipo, string? origem = null) =>
        new(osId, idCompensacao, tipo, origem);
}
