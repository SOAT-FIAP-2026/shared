using System.Text.Json;
using Fiap.TechChallenge.Contracts.BillingService;
using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class BillingWireTests
{
    [Fact]
    public void StatusPagamentoContrato_espelha_valores_do_billing()
    {
        var esperado = WireFixture.Ler("billing.PagamentoStatus")["valores"]!.AsArray()
            .Select(v => (v!["nome"]!.GetValue<string>(), v["valor"]!.GetValue<long>()));

        var atual = Enum.GetValues<StatusPagamentoContrato>()
            .Select(s => (s.ToString(), (long)s));

        atual.Should().Equal(esperado);
    }

    [Fact]
    public void OrcamentoResponse_espelha_entidade_Orcamento() =>
        WireFixture.DeveEspelhar<OrcamentoResponse>("billing.Orcamento", WireJsonOptions.Http);

    [Fact]
    public void CriarBoletoRequest_endereco_continua_opcional_no_binding()
    {
        // O billing faz binding direto deste tipo com [ApiController]: um Endereco não-nulável viraria
        // [Required] implícito e trocaria o 400 ErrorResponse do serviço por um 400 ValidationProblem.
        var forma = WireShape.Extrair(typeof(CriarBoletoRequest), WireJsonOptions.Http);

        forma["endereco"]!["$nullable"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void Webhook_le_id_numerico_como_texto()
    {
        var req = JsonSerializer.Deserialize<MercadoPagoWebhookRequest>(
            """{"data":{"id":123}}""", WireJsonOptions.Http)!;

        req.Data!.Id.Should().Be("123");
    }

    [Fact]
    public void Webhook_le_id_texto_e_status_na_raiz()
    {
        var req = JsonSerializer.Deserialize<MercadoPagoWebhookRequest>(
            """{"action":"payment.updated","type":"payment","data":{"id":"abc"},"status":"approved"}""",
            WireJsonOptions.Http)!;

        req.Action.Should().Be("payment.updated");
        req.Type.Should().Be("payment");
        req.Data!.Id.Should().Be("abc");
        req.Status.Should().Be("approved");
    }

    [Fact]
    public void Webhook_le_id_na_raiz()
    {
        var req = JsonSerializer.Deserialize<MercadoPagoWebhookRequest>("""{"id":9}""", WireJsonOptions.Http)!;

        req.Id.Should().Be("9");
        req.Data.Should().BeNull();
    }

    [Fact]
    public void Parametros_do_webhook()
    {
        MercadoPagoWebhookParametros.HeaderAssinatura.Should().Be("x-signature");
        MercadoPagoWebhookParametros.HeaderRequestId.Should().Be("x-request-id");
        MercadoPagoWebhookParametros.ChaveTimestamp.Should().Be("ts");
        MercadoPagoWebhookParametros.ChaveAssinatura.Should().Be("v1");
        MercadoPagoWebhookParametros.QueryDataId.Should().Be("data.id");
        MercadoPagoWebhookParametros.QueryId.Should().Be("id");
        string.Format(MercadoPagoWebhookParametros.FormatoManifesto, "1", "r", "t")
            .Should().Be("id:1;request-id:r;ts:t;");
    }

    [Fact]
    public void Rotas_do_billing()
    {
        BillingApiRoutes.DecisaoOrcamento.Should().Be("api/v1/orcamento/{0}/decisao");
        BillingApiRoutes.ParametroAprovado.Should().Be("aprovado");
        BillingApiRoutes.ParametroIdOrdemServico.Should().Be("idOrdemServico");
        BillingApiRoutes.Ping.Should().Be("api/ping");
    }
}
