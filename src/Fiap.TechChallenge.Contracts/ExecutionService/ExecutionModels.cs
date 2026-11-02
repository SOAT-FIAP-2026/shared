namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>Serviço sugerido pelo mecânico no laudo diagnóstico (ID do catálogo do Core Service).</summary>
public sealed record ServicoSugeridoInfo(Guid IdServico, string? Descricao = null);

/// <summary>Peça/insumo sugerido pelo mecânico no laudo diagnóstico (ID do catálogo do Core Service).</summary>
public sealed record PecaSugeridaInfo(Guid IdPeca, int Quantidade, string? Descricao = null);

