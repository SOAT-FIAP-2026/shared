using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class CommonWireTests
{
    [Theory]
    [InlineData("core.ErrorResponse")]
    [InlineData("billing.ErrorResponse")]
    public void ErrorResponse_espelha_fase1_e_billing(string fixture) =>
        WireFixture.DeveEspelhar<ErrorResponse>(fixture, WireJsonOptions.Http);

    [Fact]
    public void ProblemDetailsResponse_tem_chaves_do_execution()
    {
        var forma = WireShape.Extrair(typeof(ProblemDetailsResponse), WireJsonOptions.Http);

        forma.Select(p => p.Key).Should().BeEquivalentTo(
            ["type", "title", "status", "detail", "instance", "traceId", "correlationId"]);
        forma["type"]!.GetValue<string>().Should().Be("string?");
        forma["correlationId"]!.GetValue<string>().Should().Be("string?");
    }

    [Fact]
    public void SnsMessageAttributes_TipoCompensacao_deve_ser_tipo() =>
        SnsMessageAttributes.TipoCompensacao.Should().Be("tipo");
}
