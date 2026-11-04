namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>Serviço sugerido pelo mecânico no laudo diagnóstico (ID do catálogo do Core Service).</summary>
public record ServicoSugeridoInfo(Guid IdServico, string? Descricao = null);

/// <summary>Compatibilidade com o modelo de item sugerido utilizado no execution-service.</summary>
public sealed record ServicoSugeridoItemEvent(Guid IdServico, string? Descricao = null)
    : ServicoSugeridoInfo(IdServico, Descricao);

/// <summary>Peça/insumo sugerido pelo mecânico no laudo diagnóstico (ID do catálogo do Core Service).</summary>
public record PecaSugeridaInfo(Guid IdPeca, int Quantidade, string? Descricao = null);

/// <summary>Compatibilidade com o modelo de peça sugerida utilizado no execution-service.</summary>
public sealed record PecaSugeridaItemEvent(Guid IdPeca, int Quantidade, string? Descricao = null)
    : PecaSugeridaInfo(IdPeca, Quantidade, Descricao);
