namespace Fiap.TechChallenge.Contracts.OsService;

/// <summary>
/// Códigos canônicos dos status da Ordem de Serviço em todo o ecossistema.
/// </summary>
public static class OrdemServicoStatus
{
    public const string RECEBIDA = "RECEBIDA";
    public const string EM_DIAGNOSTICO = "EM_DIAGNOSTICO";
    public const string AGUARDANDO_APROVACAO = "AGUARDANDO_APROVACAO";
    public const string APROVADA = "APROVADA";
    public const string REJEITADA = "REJEITADA";
    public const string CANCELADA = "CANCELADA";
    public const string EM_EXECUCAO = "EM_EXECUCAO";
    public const string FINALIZADA = "FINALIZADA";
    public const string ENTREGUE = "ENTREGUE";

    // Aliases compatíveis com o domínio do os-microservice
    public const string ORCAMENTO_APROVADO = "ORCAMENTO_APROVADO";
    public const string ORCAMENTO_REPROVADO = "ORCAMENTO_REPROVADO";
}
