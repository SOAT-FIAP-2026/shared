namespace Fiap.TechChallenge.Contracts.OsService;

/// <summary>
/// Códigos dos status da Ordem de Serviço.
/// </summary>
/// <remarks>
/// Eventos levam o <c>Codigo</c>; as respostas HTTP do <c>os-microservice</c> levam <c>IdStatus</c> e a
/// <c>Descricao</c> (<c>StatusDescricao</c>). O catálogo completo está em <see cref="Catalogo"/>.
/// </remarks>
public static class OrdemServicoStatus
{
    public const string RECEBIDA = "RECEBIDA";
    public const string EM_DIAGNOSTICO = "EM_DIAGNOSTICO";
    public const string AGUARDANDO_APROVACAO = "AGUARDANDO_APROVACAO";

    /// <remarks>Não trafega hoje: o <c>os-microservice</c> não emite esse código (usa <see cref="ORCAMENTO_APROVADO"/>).</remarks>
    public const string APROVADA = "APROVADA";

    /// <remarks>Não trafega hoje: o <c>os-microservice</c> não emite esse código (usa <see cref="ORCAMENTO_REPROVADO"/>).</remarks>
    public const string REJEITADA = "REJEITADA";

    public const string CANCELADA = "CANCELADA";
    public const string EM_EXECUCAO = "EM_EXECUCAO";
    public const string FINALIZADA = "FINALIZADA";
    public const string ENTREGUE = "ENTREGUE";
    public const string ORCAMENTO_APROVADO = "ORCAMENTO_APROVADO";
    public const string ORCAMENTO_REPROVADO = "ORCAMENTO_REPROVADO";

    /// <summary>
    /// Catálogo de status do <c>os-microservice</c>, na ordem declarada em
    /// <c>os-microservice/src/OrdemServico.Lambda/Domain/Entities/StatusOS.cs</c>.
    /// </summary>
    public static IReadOnlyList<StatusOrdemServicoInfo> Catalogo { get; } =
    [
        new(Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"), RECEBIDA, "Recebida"),
        new(Guid.Parse("d3e4f5a6-b7c8-9012-defa-234567890123"), EM_DIAGNOSTICO, "Em Diagnóstico"),
        new(Guid.Parse("e3f4a5b6-c7d8-9012-efab-345678901234"), AGUARDANDO_APROVACAO, "Aguardando aprovação"),
        new(Guid.Parse("a7b8c9d0-e1f2-3456-abcd-567890123456"), ORCAMENTO_APROVADO, "Orçamento Aprovado"),
        new(Guid.Parse("b8c9d0e1-f2a3-4567-bcde-678901234567"), ORCAMENTO_REPROVADO, "Orçamento Reprovado"),
        new(Guid.Parse("f4a5b6c7-d8e9-0123-efab-345678901234"), EM_EXECUCAO, "Em Execução"),
        new(Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"), FINALIZADA, "Finalizada"),
        new(Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"), ENTREGUE, "Entregue"),
        new(Guid.Parse("f6a7b8c9-d0e1-2345-fabc-456789012345"), CANCELADA, "Cancelada"),
    ];
}

/// <summary>Item do catálogo de status da OS (espelho de <c>StatusOS</c> do <c>os-microservice</c>).</summary>
public sealed record StatusOrdemServicoInfo(Guid Id, string Codigo, string Descricao);
