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
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando itens do diagnóstico técnico falham na validação de catálogo
/// ou na Saga de reserva de estoque junto ao <c>fase1-tech-challenge</c> (Core).
/// Consumido pelo <c>execution-service</c> para retornar a ordem ao status <c>EM_DIAGNOSTICO</c>.
/// </summary>
public sealed record DiagnosticoRejeitadoEvent : IntegrationEvent
{
    public required string Motivo { get; init; }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> após validar os itens do diagnóstico no catálogo do Core,
/// reservar as peças no estoque e calcular o valor da OS (status <c>AGUARDANDO_APROVACAO</c>).
/// Consumido pelo <c>billing-service</c> para gerar o orçamento e disponibilizar a cobrança Mercado Pago.
/// </summary>
public sealed record DiagnosticoFinalizadoEvent : IntegrationEvent
{
    public required IReadOnlyList<ItemOrcamentoInfo> Itens { get; init; }
    public required decimal Total { get; init; }
    public required ClienteInfo Cliente { get; init; }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> após o cliente aprovar o orçamento e o <c>billing-service</c>
/// confirmar o pagamento (status <c>ORCAMENTO_APROVADO</c>).
/// Consumido pelo <c>execution-service</c> para inserir a ordem na fila FIFO (<c>NA_FILA</c>).
/// </summary>
public sealed record OrdemServicoLiberadaParaExecucaoEvent : IntegrationEvent
{
    public required IReadOnlyList<TarefaInfo> Tarefas { get; init; }
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
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> quando o cliente reprova o orçamento.
/// Dispara o estorno das peças reservadas no estoque do Core (<c>fase1-tech-challenge</c>).
/// </summary>
public sealed record OrcamentoReprovadoEvent : IntegrationEvent
{
    public required IReadOnlyList<Guid> PecasInsumosIds { get; init; }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> ao concluir a Saga de reserva de peças/insumos no estoque.
/// </summary>
public sealed record PecasInsumoIncluidasEvent : IntegrationEvent
{
    public required Guid IdSaga { get; init; }
    public required IReadOnlyList<Guid> PecasInsumosIds { get; init; }
}

/// <summary>
/// Publicado pelo <c>os-microservice</c> a cada mudança de status da Ordem de Serviço.
/// </summary>
public sealed record StatusOrdemServicoAlteradoEvent : IntegrationEvent
{
    public required string StatusAnterior { get; init; }
    public required string StatusAtual { get; init; }
}

