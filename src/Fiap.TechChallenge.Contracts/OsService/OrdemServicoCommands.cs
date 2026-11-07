namespace Fiap.TechChallenge.Contracts.OsService;

using ComandoCompensacao = ComandoCompensacaoContrato;
using TipoCompensacao = TipoCompensacaoContrato;

/// <summary>
/// Tipos de comando de compensação processados pela fila SQS
/// <c>techchallenge-ordem-servico-compensacao-&lt;stage&gt;</c> no <c>os-microservice</c>.
/// </summary>
public enum TipoCompensacaoContrato
{
    EstornarEstoque,
    RemoverPecaInsumo,
    CancelarOrdemServico
}

/// <summary>
/// Contrato de comando enviado via SQS para a <c>CompensacaoFilaFunction</c> do <c>os-microservice</c>.
/// </summary>
public sealed record ComandoCompensacaoContrato(
    string IdCompensacao,
    TipoCompensacaoContrato Tipo,
    Guid IdOrdemServico,
    IReadOnlyCollection<Guid> PecasInsumosIds,
    string Motivo,
    Guid? IdSaga = null,
    string? CorrelationId = null);
