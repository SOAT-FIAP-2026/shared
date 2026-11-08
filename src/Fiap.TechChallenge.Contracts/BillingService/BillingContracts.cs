namespace Fiap.TechChallenge.Contracts.BillingService;

/// <summary>
/// Rotas da API HTTP de Pagamentos e Orçamentos do <c>billing-service</c>, espelho de
/// <c>OrcamentoController</c> e <c>PagamentoController</c>.
/// </summary>
/// <remarks>O <c>billing-service</c> não exige autenticação em nenhuma rota.</remarks>
public static class BillingApiRoutes
{
    /// <summary>POST, corpo <see cref="CriarBoletoRequest"/>, 201 <see cref="BoletoResponse"/>.</summary>
    public const string CriarBoleto = "api/v1/pagamento/boleto";

    /// <summary>GET, 200 <see cref="BoletoResponse"/> ou 404 sem corpo.</summary>
    public const string ObterPagamentoPorId = "api/v1/pagamento/{0}";

    /// <summary>POST do Mercado Pago, corpo <see cref="MercadoPagoWebhookRequest"/>.</summary>
    public const string WebhookMercadoPago = "api/v1/pagamento/webhook";

    /// <summary>POST, query <see cref="ParametroIdOrdemServico"/> opcional, corpo <see cref="GerarOrcamentoRequest"/>, 200 <see cref="OrcamentoResponse"/>.</summary>
    public const string GerarOrcamento = "api/v1/orcamento";

    /// <summary>GET, 200 <see cref="OrcamentoResponse"/> ou 404 <c>ErrorResponse</c>.</summary>
    public const string ObterOrcamentoPorId = "api/v1/orcamento/{0}";

    /// <summary>GET, query <c>page</c>/<c>pageSize</c>; devolve array simples de <see cref="OrcamentoResponse"/> (não paginado) e 404 quando vazio.</summary>
    public const string ListarOrcamentos = "api/v1/orcamento/listar";

    /// <summary>POST, corpo <see cref="EnviarOrcamentoRequest"/>, 200 com texto.</summary>
    public const string EnviarOrcamento = "api/v1/orcamento/{0}/enviar-orcamento";

    /// <summary>
    /// GET com query <see cref="ParametroAprovado"/>; devolve HTML e chama
    /// <c>PATCH api/v1/ordens-servico/{osId}/aprovar-orcamento?aprovado=</c> no <c>os-microservice</c> (502 se falhar).
    /// </summary>
    public const string DecisaoOrcamento = "api/v1/orcamento/{0}/decisao";

    /// <summary>Query de <see cref="DecisaoOrcamento"/>: <c>true</c> ou <c>false</c>.</summary>
    public const string ParametroAprovado = "aprovado";

    /// <summary>Query opcional de <see cref="GerarOrcamento"/>; ausente, o billing usa um id aleatório.</summary>
    public const string ParametroIdOrdemServico = "idOrdemServico";

    /// <summary>GET anônimo (<c>{"message":"pong"}</c>).</summary>
    public const string Ping = "api/ping";
}

/// <summary>Pedido de boleto. <see cref="Endereco"/> é obrigatório: sem ele o billing devolve 400.</summary>
/// <remarks>Se algum campo do endereço vier em branco, o endereço é omitido na chamada ao Mercado Pago.</remarks>
public sealed record CriarBoletoRequest(
    Guid IdOrcamento,
    string Email,
    string Nome,
    string Sobrenome,
    string Cpf,
    EnderecoBoletoRequest Endereco);

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

/// <summary>
/// Orçamento devolvido pelos endpoints de orçamento. Espelho da entidade <c>Orcamento</c> do <c>billing-service</c>,
/// que é serializada direto na resposta.
/// </summary>
/// <remarks><see cref="IdOrcamento"/> é sempre igual a <see cref="Id"/>.</remarks>
public sealed record OrcamentoResponse(
    Guid Id,
    Guid IdOrdemServico,
    Guid IdOrcamento,
    ValorMonetarioResponse ValorTotal,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    DateTime? ApagadoEm);

/// <summary>Valor monetário serializado como objeto (<c>{"valor": n}</c>), espelho de <c>ValorMonetarioVO</c>.</summary>
public sealed record ValorMonetarioResponse(decimal Valor);
