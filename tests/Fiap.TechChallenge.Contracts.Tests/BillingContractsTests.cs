using System.Text.Json;
using Fiap.TechChallenge.Contracts.BillingService;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class BillingContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void BillingApiRoutes_devem_conter_formatos_corretos()
    {
        var id = Guid.NewGuid();

        BillingApiRoutes.CriarBoleto.Should().Be("api/v1/pagamento/boleto");
        string.Format(BillingApiRoutes.ObterPagamentoPorId, id).Should().Be($"api/v1/pagamento/{id}");
        BillingApiRoutes.WebhookMercadoPago.Should().Be("api/v1/pagamento/webhook");
        BillingApiRoutes.GerarOrcamento.Should().Be("api/v1/orcamento");
        string.Format(BillingApiRoutes.ObterOrcamentoPorId, id).Should().Be($"api/v1/orcamento/{id}");
        BillingApiRoutes.ListarOrcamentos.Should().Be("api/v1/orcamento/listar");
        string.Format(BillingApiRoutes.EnviarOrcamento, id).Should().Be($"api/v1/orcamento/{id}/enviar-orcamento");
    }

    [Fact]
    public void Enums_de_Billing_devem_possuir_valores_esperados()
    {
        MetodoPagamentoContrato.Pix.ToString().Should().Be("Pix");
        MetodoPagamentoContrato.CartaoCredito.ToString().Should().Be("CartaoCredito");
        MetodoPagamentoContrato.Boleto.ToString().Should().Be("Boleto");

        ((int)StatusPagamentoContrato.Pendente).Should().Be(0);
        ((int)StatusPagamentoContrato.Aprovado).Should().Be(1);
        ((int)StatusPagamentoContrato.Rejeitado).Should().Be(2);
        ((int)StatusPagamentoContrato.Cancelado).Should().Be(3);
        ((int)StatusPagamentoContrato.Expirado).Should().Be(4);
    }

    [Fact]
    public void CriarBoletoRequest_e_BoletoResponse_devem_serializar_corretamente()
    {
        var orcamentoId = Guid.NewGuid();
        var endereco = new EnderecoBoletoRequest("01001000", "Praça da Sé", "1", "Sé", "São Paulo", "SP");
        var req = new CriarBoletoRequest(orcamentoId, "cliente@email.com", "Lucas", "Moraes", "12345678901", endereco);
        req.IdOrcamento.Should().Be(orcamentoId);
        req.Endereco.Should().Be(endereco);
        req.Email.Should().Be("cliente@email.com");
        req.Nome.Should().Be("Lucas");
        req.Sobrenome.Should().Be("Moraes");
        req.Cpf.Should().Be("12345678901");

        var pagId = Guid.NewGuid();
        var vencimento = DateTimeOffset.UtcNow.AddDays(3);
        var res = new BoletoResponse(
            pagId,
            "mp-pay-9988",
            "Pendente",
            350.00m,
            "https://mercadopago.com/boleto/9988",
            "34191.79001 01043.510047 91020.150008 8 98760000035000",
            vencimento);

        res.PagamentoId.Should().Be(pagId);
        res.PaymentId.Should().Be("mp-pay-9988");
        res.Status.Should().Be("Pendente");
        res.Valor.Should().Be(350.00m);
        res.BoletoUrl.Should().Be("https://mercadopago.com/boleto/9988");
        res.DataVencimento.Should().Be(vencimento);

        var json = JsonSerializer.Serialize(res, JsonOptions);
        var copy = JsonSerializer.Deserialize<BoletoResponse>(json, JsonOptions);
        copy.Should().BeEquivalentTo(res);
    }

    [Fact]
    public void GerarOrcamentoRequest_e_ItemCalculoOrcamentoDto_devem_serializar_corretamente()
    {
        var servicoId = Guid.NewGuid();
        var pecaId = Guid.NewGuid();

        var servicoItem = new ItemCalculoOrcamentoDto("Alinhamento", 120.00m, 1, 30, servicoId, "Alinhamento Dianteiro");
        var pecaItem = new ItemCalculoOrcamentoDto("Terminal de Direção", 95.50m, 2, null, pecaId);

        var request = new GerarOrcamentoRequest([servicoItem], [pecaItem]);
        request.Servicos.Should().HaveCount(1);
        request.PecasInsumo.Should().HaveCount(1);

        var enviarReq = new EnviarOrcamentoRequest("cliente@teste.com");
        enviarReq.EmailCliente.Should().Be("cliente@teste.com");

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var copy = JsonSerializer.Deserialize<GerarOrcamentoRequest>(json, JsonOptions);
        copy.Should().BeEquivalentTo(request);
    }

    [Fact]
    public void MercadoPagoWebhookRequest_deve_serializar_corretamente()
    {
        var webhook = new MercadoPagoWebhookRequest("payment.created", "payment", new MercadoPagoWebhookData("123456789"));
        webhook.Action.Should().Be("payment.created");
        webhook.Type.Should().Be("payment");
        webhook.Data!.Id.Should().Be("123456789");

        var json = JsonSerializer.Serialize(webhook, JsonOptions);
        var copy = JsonSerializer.Deserialize<MercadoPagoWebhookRequest>(json, JsonOptions);
        copy.Should().BeEquivalentTo(webhook);
    }
}
