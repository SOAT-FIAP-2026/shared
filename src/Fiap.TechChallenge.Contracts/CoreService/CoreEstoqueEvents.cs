namespace Fiap.TechChallenge.Contracts.Events;

/// <summary>
/// Publicado pelo <c>fase1-tech-challenge</c> (Estoque) quando uma reserva de peça falha de forma assíncrona.
/// Consumido pela <c>CompensacaoEventoFunction</c> do <c>os-microservice</c> (filtro SNS <c>ReservaEstoqueFalhou</c>)
/// para remover a peça da OS e recalcular o orçamento.
/// </summary>
/// <remarks>Não trafega hoje: o Core não publica no SNS. O consumidor do OS e o filtro SNS esperam o nome <c>ReservaEstoqueFalhou</c>, mas este tipo gera <c>ReservaEstoqueFalhouEvent</c>.</remarks>
public sealed record ReservaEstoqueFalhouEvent : IntegrationEvent
{
    public required Guid IdPecaInsumo { get; init; }
    public required string Motivo { get; init; }
}

