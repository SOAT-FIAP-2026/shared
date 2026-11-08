namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>Dados mínimos do cliente (snapshot sem CPF/CNPJ — LGPD).</summary>
public record ClienteInfo(Guid IdCliente, string Nome, string? Telefone, string? Email = null);

/// <summary>Contrato de cliente compatível com o execution-service.</summary>
/// <remarks>Não trafega hoje: nenhum serviço usa este tipo (o execution-service lê <see cref="ClienteInfo"/>).</remarks>
public sealed record ClienteContrato(Guid IdCliente, string Nome, string? Telefone)
    : ClienteInfo(IdCliente, Nome, Telefone);

/// <summary>Dados mínimos do veículo para identificação no pátio da oficina.</summary>
public record VeiculoInfo(string Placa, string Modelo, int Ano);

/// <summary>Contrato de veículo compatível com o execution-service.</summary>
/// <remarks>Não trafega hoje: nenhum serviço usa este tipo (o execution-service lê <see cref="VeiculoInfo"/>).</remarks>
public sealed record VeiculoContrato(string Placa, string Modelo, int Ano)
    : VeiculoInfo(Placa, Modelo, Ano);

/// <summary>Tarefa de reparo aprovada e liberada para a fila de produção no Execution Service.</summary>
public record TarefaInfo(Guid IdServico, string Descricao, int TempoEstimadoMinutos)
{
    /// <summary>Alias de compatibilidade com execution-service.</summary>
    public Guid IdServicoOrigem => IdServico;
}

/// <summary>Contrato de tarefa compatível com o execution-service.</summary>
/// <remarks>Não trafega hoje: nenhum serviço usa este tipo; no fio vai <see cref="TarefaInfo"/>, que precisa de <c>idServico</c>.</remarks>
public sealed record TarefaItemContrato(Guid IdServicoOrigem, string Descricao, int TempoEstimadoMinutos)
    : TarefaInfo(IdServicoOrigem, Descricao, TempoEstimadoMinutos);

/// <summary>Item precificado (serviço ou peça/insumo) enviado para geração do orçamento no Billing Service.</summary>
/// <remarks>Só aparece em <see cref="DiagnosticoFinalizadoEvent"/>, que não tem produtor hoje.</remarks>
public sealed record ItemOrcamentoInfo(Guid IdItem, string TipoItem, string Descricao, int Quantidade, decimal ValorUnitario)
{
    public decimal ValorTotal => Math.Round(Quantidade * ValorUnitario, 2);
}
