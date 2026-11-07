using System.Net;
using Fiap.TechChallenge.E2ETests.Support;
using FluentAssertions;
using Reqnroll;

namespace Fiap.TechChallenge.E2ETests.StepDefinitions
{
    [Binding]
    public class SanidadeSteps
    {
        private readonly AwsFlociHelper _floci;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private HttpResponseMessage? _resCore;
        private HttpResponseMessage? _resOs;
        private HttpResponseMessage? _resExec;
        private HttpResponseMessage? _resBilling;

        private const string CorrelationHeader = "X-Correlation-ID";
        private readonly string _sanityCorrelationId = $"e2e-sanity-{Guid.NewGuid():N}";

        public SanidadeSteps(AwsFlociHelper floci)
        {
            _floci = floci;
            _http.DefaultRequestHeaders.Remove(CorrelationHeader);
            _http.DefaultRequestHeaders.Add(CorrelationHeader, _sanityCorrelationId);
        }

        [When(@"consulto os endpoints de health dos serviços Core, OS e Execution")]
        [When(@"consulto os endpoints de health dos serviços Core, OS, Execution e Billing")]
        public async Task QuandoConsultoOsEndpointsDeHealthDosServicosCoreOsEExecution()
        {
            _resCore = await _http.GetAsync($"{_config.CoreApiUrl}/health/live");
            _resOs = await _http.GetAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/health");
            _resExec = await _http.GetAsync($"{_config.ExecutionApiUrl}/health/live");
            _resBilling = await _http.GetAsync($"{_config.BillingApiUrl}/health");
        }

        [Then(@"todos os serviços retornam status saudável")]
        public void EntaoTodosOsServicosRetornamStatusSaudavel()
        {
            _resCore.Should().NotBeNull();
            _resCore!.StatusCode.Should().Be(HttpStatusCode.OK, "Core Service deve responder HTTP 200");
            _resCore.Headers.TryGetValues(CorrelationHeader, out var coreCorr).Should().BeTrue();
            coreCorr!.First().Should().Be(_sanityCorrelationId);

            _resOs.Should().NotBeNull();
            _resOs!.StatusCode.Should().Be(HttpStatusCode.OK, "OS Service deve responder HTTP 200");
            _resOs.Headers.TryGetValues(CorrelationHeader, out var osCorr).Should().BeTrue();
            osCorr!.First().Should().Be(_sanityCorrelationId);

            _resExec.Should().NotBeNull();
            _resExec!.StatusCode.Should().Be(HttpStatusCode.OK, "Execution Service deve responder HTTP 200");
            _resExec.Headers.TryGetValues(CorrelationHeader, out var execCorr).Should().BeTrue();
            execCorr!.First().Should().Be(_sanityCorrelationId);

            _resBilling.Should().NotBeNull();
            _resBilling!.StatusCode.Should().Be(HttpStatusCode.OK, "Billing Service deve responder HTTP 200");
            _resBilling.Headers.TryGetValues(CorrelationHeader, out var billingCorr).Should().BeTrue();
            billingCorr!.First().Should().Be(_sanityCorrelationId);
        }

        [Then(@"os recursos no Floci \(DynamoDB, tópicos SNS e filas SQS\) estão acessíveis")]
        public async Task EntaoOsRecursosNoFlociDynamoDbTopicosSnsEFilasSqsEstaoAcessiveis()
        {
            // Valida tabela DynamoDB
            var tabelaExiste = await _floci.TabelaExiste(_config.TableName);
            tabelaExiste.Should().BeTrue($"A tabela {_config.TableName} deve existir no Floci");

            // Valida tópicos SNS
            var topicos = await _floci.ListarTopicosSns();
            topicos.Should().Contain(t => t.Contains("techchallenge-eventos-local"), "O tópico SNS techchallenge-eventos-local deve existir");

            // Valida filas SQS
            var filas = await _floci.ListarFilasSqs();
            filas.Should().NotBeEmpty("Deve haver filas SQS criadas no Floci");
            filas.Should().Contain(f => f.Contains("techchallenge-os-eventos-local"));
            filas.Should().Contain(f => f.Contains("techchallenge-execucao-eventos-local"));
            filas.Should().Contain(f => f.Contains("techchallenge-billing-eventos-local"));
            filas.Should().Contain(f => f.Contains("techchallenge-billing-eventos-dlq-local"));
        }
    }
}

