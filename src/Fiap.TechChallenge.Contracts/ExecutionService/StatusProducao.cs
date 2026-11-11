namespace Fiap.TechChallenge.Contracts.ExecutionService;

/// <summary>
/// Códigos de status da ordem de produção no fio (UPPER_SNAKE), espelho de <c>StatusOrdemProducao</c> do
/// <c>execution-service</c>. Só aparecem nas respostas HTTP, nunca em eventos.
/// </summary>
/// <remarks>
/// Valores numéricos do domínio, aceitos também no filtro <c>?status=</c> da fila (junto com o nome PascalCase):
/// 1 AguardandoDiagnostico, 2 EmDiagnostico, 3 DiagnosticoConcluido, 4 NaFila, 5 EmExecucao, 6 Concluida, 7 Cancelada.
/// </remarks>
public static class StatusOrdemProducaoContrato
{
    public const string AGUARDANDO_DIAGNOSTICO = "AGUARDANDO_DIAGNOSTICO";
    public const string EM_DIAGNOSTICO = "EM_DIAGNOSTICO";
    public const string DIAGNOSTICO_CONCLUIDO = "DIAGNOSTICO_CONCLUIDO";
    public const string NA_FILA = "NA_FILA";
    public const string EM_EXECUCAO = "EM_EXECUCAO";
    public const string CONCLUIDA = "CONCLUIDA";
    public const string CANCELADA = "CANCELADA";
}

/// <summary>
/// Códigos de status da tarefa de produção no fio (UPPER_SNAKE), espelho de <c>StatusTarefaProducao</c> do
/// <c>execution-service</c>.
/// </summary>
/// <remarks>Valores numéricos do domínio: 1 Pendente, 2 EmExecucao, 3 Concluida, 4 Interrompida.</remarks>
public static class StatusTarefaProducaoContrato
{
    public const string PENDENTE = "PENDENTE";
    public const string EM_EXECUCAO = "EM_EXECUCAO";
    public const string CONCLUIDA = "CONCLUIDA";
    public const string INTERROMPIDA = "INTERROMPIDA";
}
