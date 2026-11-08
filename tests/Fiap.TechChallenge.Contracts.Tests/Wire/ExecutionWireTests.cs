using System.Text.RegularExpressions;
using Fiap.TechChallenge.Contracts.Common;
using Fiap.TechChallenge.Contracts.ExecutionService;
using Fiap.TechChallenge.Contracts.ExecutionService.Responses;
using Fiap.TechChallenge.Contracts.Tests.Support;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests.Wire;

public class ExecutionWireTests
{
    public static TheoryData<string, Type> Respostas => new()
    {
        { "OrdemProducaoResponse", typeof(OrdemProducaoResponse) },
        { "ClienteSnapshotResponse", typeof(ClienteSnapshotResponse) },
        { "VeiculoSnapshotResponse", typeof(VeiculoSnapshotResponse) },
        { "DiagnosticoResponse", typeof(DiagnosticoResponse) },
        { "ServicoSugeridoResponse", typeof(ServicoSugeridoResponse) },
        { "PecaSugeridaResponse", typeof(PecaSugeridaResponse) },
        { "TarefaProducaoResponse", typeof(TarefaProducaoResponse) },
        { "HistoricoProducaoResponse", typeof(HistoricoProducaoResponse) },
        { "OrdemProducaoFilaItemResponse", typeof(OrdemProducaoFilaItemResponse) },
        { "RelatorioProdutividadeResponse", typeof(RelatorioProdutividadeResponse) },
        { "MetricaProdutividadeMecanicoResponse", typeof(MetricaProdutividadeMecanicoResponse) },
    };

    [Theory]
    [MemberData(nameof(Respostas))]
    public void Resposta_do_execution_espelha_servico(string nome, Type tipo) =>
        WireFixture.DeveEspelhar(tipo, $"execution.{nome}", WireJsonOptions.Http);

    [Theory]
    [MemberData(nameof(Respostas))]
    public void Tipos_homonimos_ficam_em_sub_namespace(string nome, Type tipo)
    {
        tipo.Name.Should().Be(nome);
        tipo.Namespace.Should().Be("Fiap.TechChallenge.Contracts.ExecutionService.Responses");
    }

    [Theory]
    [InlineData("execution.StatusOrdemProducao", typeof(StatusOrdemProducaoContrato))]
    [InlineData("execution.StatusTarefaProducao", typeof(StatusTarefaProducaoContrato))]
    public void Status_de_producao_espelha_enum(string fixture, Type constantes)
    {
        var esperado = WireFixture.Ler(fixture)["valores"]!.AsArray()
            .Select(v => ParaUpperSnake(v!["nome"]!.GetValue<string>()));

        var atual = constantes.GetFields()
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);

        atual.Should().Equal(esperado);
    }

    [Fact]
    public void Parametros_de_query_do_execution()
    {
        ExecutionApiRoutes.ParametroStatus.Should().Be("status");
        ExecutionApiRoutes.ParametroFilialId.Should().Be("filialId");
        ExecutionApiRoutes.ParametroDe.Should().Be("de");
        ExecutionApiRoutes.ParametroAte.Should().Be("ate");
        ExecutionApiRoutes.ParametroIdMecanico.Should().Be("idMecanico");
    }

    private static string ParaUpperSnake(string pascal) =>
        Regex.Replace(pascal, "(?<!^)([A-Z])", "_$1").ToUpperInvariant();
}
