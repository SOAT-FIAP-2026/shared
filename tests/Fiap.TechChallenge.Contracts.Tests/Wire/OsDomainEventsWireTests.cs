using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.OsService.DomainEvents;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class OsDomainEventsWireTests
{
    public static TheoryData<string, Type> Eventos => new()
    {
        { "OrdemServicoCriada", typeof(OrdemServicoCriada) },
        { "StatusOrdemServicoAlterado", typeof(StatusOrdemServicoAlterado) },
        { "PecasInsumoIncluidas", typeof(PecasInsumoIncluidas) },
        { "OrcamentoReprovado", typeof(OrcamentoReprovado) },
        { "OrdemServicoCancelada", typeof(OrdemServicoCancelada) },
        { "CompensacaoExecutada", typeof(CompensacaoExecutada) },
    };

    [Theory]
    [MemberData(nameof(Eventos))]
    public void Evento_de_dominio_espelha_outbox_do_OS(string nome, Type tipo) =>
        WireFixture.DeveEspelhar(tipo, $"os.{nome}", WireJsonOptions.OsEventos);

    [Fact]
    public void TipoEvento_nao_tem_sufixo_Event() =>
        new OrcamentoReprovado(Guid.NewGuid(), []).TipoEvento.Should().Be("OrcamentoReprovado");

    [Theory]
    [MemberData(nameof(Eventos))]
    public void Tipos_homonimos_ficam_em_sub_namespace(string nome, Type tipo)
    {
        tipo.Name.Should().Be(nome);
        tipo.Namespace.Should().Be("Fiap.TechChallenge.Contracts.OsService.DomainEvents");
    }
}
