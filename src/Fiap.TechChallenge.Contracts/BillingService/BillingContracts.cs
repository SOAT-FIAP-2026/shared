namespace Fiap.TechChallenge.Contracts.BillingService;

/// <summary>
/// Rotas canônicas da API HTTP de Pagamentos e Orçamentos (/api/v1/pagamento e /api/v1/orcamento).
/// </summary>
public static class BillingApiRoutes
{
    public const string CriarBoleto = "api/v1/pagamento/boleto";
    public const string ObterPagamentoPorId = "api/v1/pagamento/{0}";
    public const string WebhookMercadoPago = "api/v1/pagamento/webhook";
    public const string GerarOrcamento = "api/v1/orcamento";
    public const string ObterOrcamentoPorId = "api/v1/orcamento/{0}";
    public const string ListarOrcamentos = "api/v1/orcamento/listar";
    public const string EnviarOrcamento = "api/v1/orcamento/{0}/enviar-orcamento";
}

public sealed record CriarBoletoRequest(
    Guid IdOrcamento,
    string Email,
    string Nome,
    string Sobrenome,
    string Cpf,
    EnderecoBoletoRequest? Endereco = null);

public sealed record EnderecoBoletoRequest(
    string Cep,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Estado);

public sealed record BoletoResponse(
    Guid PagamentoId,
    string PaymentId,
    string Status,
    decimal Valor,
    string? BoletoUrl,
    string? CodigoBarras,
    DateTimeOffset? DataVencimento);

public sealed record EnviarOrcamentoRequest(
    string EmailCliente = "");

public sealed record ItemCalculoOrcamentoDto(
    string Descricao,
    decimal ValorUnitario,
    int Quantidade = 1,
    int? TempoEstimadoMinutos = null,
    Guid? IdItem = null,
    string? Nome = null);

public sealed record GerarOrcamentoRequest
{
    public IReadOnlyCollection<ItemCalculoOrcamentoDto> Servicos { get; init; } = [];
    public IReadOnlyCollection<ItemCalculoOrcamentoDto> PecasInsumo { get; init; } = [];

    public GerarOrcamentoRequest() { }

    public GerarOrcamentoRequest(
        IReadOnlyCollection<ItemCalculoOrcamentoDto>? servicos,
        IReadOnlyCollection<ItemCalculoOrcamentoDto>? pecasInsumo = null)
    {
        Servicos = servicos ?? [];
        PecasInsumo = pecasInsumo ?? [];
    }
}
