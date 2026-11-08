using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class PacoteTests
{
    [Fact]
    public void Versao_do_pacote_deve_ser_2_0_0() =>
        typeof(IntegrationEvent).Assembly.GetName().Version!.ToString(3).Should().Be("2.0.0");
}
