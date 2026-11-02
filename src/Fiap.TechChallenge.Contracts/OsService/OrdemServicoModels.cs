namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>Dados mínimos do cliente (snapshot sem CPF/CNPJ — LGPD).</summary>
public sealed record ClienteInfo(Guid IdCliente, string Nome, string? Telefone, string? Email = null);

/// <summary>Dados mínimos do veículo para identificação no pátio da oficina.</summary>
public sealed record VeiculoInfo(string Placa, string Modelo, int Ano);

/// <summary>Tarefa de reparo aprovada e liberada para a fila de produção no Execution Service.</summary>
public sealed record TarefaInfo(Guid IdServico, string Descricao, int TempoEstimadoMinutos);

/// <summary>Item precificado (serviço ou peça/insumo) enviado para geração do orçamento no Billing Service.</summary>
public sealed record ItemOrcamentoInfo(Guid IdItem, string TipoItem, string Descricao, int Quantidade, decimal ValorUnitario)
{
    public decimal ValorTotal => Math.Round(Quantidade * ValorUnitario, 2);
}

