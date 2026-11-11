namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Corpo de erro (<c>application/json</c>, camelCase) devolvido pelo middleware de exceções do
/// <c>fase1-tech-challenge</c>, do <c>os-microservice</c> e do <c>billing-service</c>.
/// </summary>
/// <remarks>
/// Os três serviços usam a mesma forma; muda só o mapeamento de status:
/// fase1 (Domain/Argument/InvalidOperation → 400, UnauthorizedAccess → 401, resto → 500);
/// OS acrescenta concorrência → 409 e <c>BadHttpRequestException</c> → próprio status;
/// billing acrescenta <c>*NaoEncontradoException</c> → 404 e <c>HttpRequestException</c> → 4xx/502.
/// <see cref="TraceId"/> vem de <c>Activity.TraceId</c> ou do <c>TraceIdentifier</c>.
/// </remarks>
public sealed record ErrorResponse(int StatusCode, string Message, string TraceId);

/// <summary>
/// Corpo de erro RFC 7807 (<c>application/problem+json</c>) devolvido pelo <c>execution-service</c>.
/// </summary>
/// <remarks>
/// <see cref="Type"/> é omitido quando nulo; <see cref="CorrelationId"/> só aparece quando presente no request.
/// Mapeamento: não encontrada → 404, transição inválida/concorrência → 409, mecânico não autorizado → 403,
/// Domain/Argument → 400, UnauthorizedAccess → 401, resto (inclusive <c>InvalidOperationException</c>) → 500.
/// </remarks>
public sealed record ProblemDetailsResponse(
    string? Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string TraceId,
    string? CorrelationId);
