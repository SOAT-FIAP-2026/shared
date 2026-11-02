using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fiap.TechChallenge.E2ETests.Support;
using FluentAssertions;
using Reqnroll;

namespace Fiap.TechChallenge.E2ETests.StepDefinitions
{
    [Binding]
    public class CicloCompletoOrdemServicoSteps
    {
        private readonly E2ETestContext _context;
        private readonly AwsFlociHelper _floci;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public CicloCompletoOrdemServicoSteps(E2ETestContext context, AwsFlociHelper floci)
        {
            _context = context;
            _floci = floci;
        }

        private void SetAuthHeader(HttpClient client)
        {
            if (!string.IsNullOrEmpty(_context.AdminToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _context.AdminToken);
            }
        }

        [Given(@"que os microsserviços Core, OS e Execução estão operacionais")]
        [Given(@"que os microsserviços Core e OS estão operacionais")]
        public async Task DadoQueOsMicrosservicosEstaoOperacionais()
        {
            var resCore = await _http.GetAsync($"{_config.CoreApiUrl}/health/live");
            resCore.IsSuccessStatusCode.Should().BeTrue("Core Service deve estar operacional");

            var resOs = await _http.GetAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/health");
            resOs.IsSuccessStatusCode.Should().BeTrue("OS Service deve estar operacional");

            var resExec = await _http.GetAsync($"{_config.ExecutionApiUrl}/health/live");
            resExec.IsSuccessStatusCode.Should().BeTrue("Execution Service deve estar operacional");
        }

        [Given(@"que o usuário autentica como Administrador no Core Service")]
        public async Task DadoQueOUsuarioAutenticaComoAdministradorNoCoreService()
        {
            var loginPayload = new { login = _config.AdminLogin, senha = _config.AdminSenha };
            var response = await _http.PostAsJsonAsync($"{_config.CoreApiUrl}/api/v1/autenticacao/login", loginPayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Login do admin deve ser bem sucedido");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            _context.AdminToken = json.GetProperty("token").GetString();
            _context.AdminToken.Should().NotBeNullOrWhiteSpace("Token JWT de admin deve ser retornado");
        }

        [Given(@"que existe um cliente e um veículo cadastrados no catálogo")]
        public async Task DadoQueExisteUmClienteEUmVeiculoCadastradosNoCatalogo()
        {
            SetAuthHeader(_http);

            // Obtém primeiro cliente
            var clientes = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/clientes");
            clientes.Should().NotBeNullOrEmpty("Deve haver clientes cadastrados no catálogo");
            var cliente = clientes![0];
            _context.ClienteId = Guid.Parse(cliente.GetProperty("id").GetString()!);
            _context.ClienteNome = cliente.GetProperty("nome").GetString()!;

            // Obtém primeiro veículo (pode vir como array ou paginado em "items")
            var veiculosDoc = await _http.GetFromJsonAsync<JsonDocument>($"{_config.CoreApiUrl}/api/v1/veiculos");
            var veiculosRoot = veiculosDoc!.RootElement;
            JsonElement veiculo = veiculosRoot.ValueKind == JsonValueKind.Array
                ? veiculosRoot[0]
                : veiculosRoot.GetProperty("items")[0];

            _context.VeiculoId = Guid.Parse(veiculo.GetProperty("id").GetString()!);
            _context.VeiculoPlaca = veiculo.GetProperty("placa").GetString()!;

            // Obtém primeiro serviço e primeira peça do catálogo
            var servicos = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/servicos");
            servicos.Should().NotBeNullOrEmpty("Deve haver serviços cadastrados");
            _context.ServicoId = Guid.Parse(servicos![0].GetProperty("id").GetString()!);

            var pecas = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/pecas-insumos");
            pecas.Should().NotBeNullOrEmpty("Deve haver peças cadastradas");
            _context.PecaId = Guid.Parse(pecas![0].GetProperty("id").GetString()!);
        }

        [When(@"o atendente solicita a abertura de uma nova Ordem de Serviço no OS Service")]
        public async Task QuandoOAtendenteSolicitaAAberturaDeUmaNovaOrdemDeServicoNoOsService()
        {
            SetAuthHeader(_http);

            var request = new
            {
                clienteId = _context.ClienteId,
                veiculoId = _context.VeiculoId,
                observacao = "Abertura E2E BDD Test"
            };

            var response = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico", request);
            _context.UltimaResposta = response;
            _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, "Abertura da OS deve retornar 201 Created");

            var json = JsonSerializer.Deserialize<JsonElement>(_context.UltimoCorpoResposta);
            _context.OrdemServicoId = Guid.Parse(json.GetProperty("id").GetString()!);
            _context.OrdemServicoId.Should().NotBeEmpty();
        }

        [Then(@"a Ordem de Serviço é criada com sucesso com status ""(.*)""")]
        public async Task EntaoAOrdemDeServicoECriadaComSucessoComStatus(string statusEsperado)
        {
            SetAuthHeader(_http);

            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            var statusDesc = os.GetProperty("statusDescricao").GetString();
            statusDesc.Should().BeEquivalentTo(statusEsperado, "O status retornado na API deve bater com o esperado");
        }

        [Then(@"a Ordem de Serviço pode ser consultada no DynamoDB")]
        public async Task EntaoAOrdemDeServicoPodeSerConsultadaNoDynamoDB()
        {
            var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
            item.Should().NotBeNull("Item correspondente à OS deve existir na tabela DynamoDB");
            item!["PK"].S.Should().Be($"OS#{_context.OrdemServicoId}");
            item["statusCodigo"].S.Should().Be("RECEBIDA");
            item["tipoEntidade"].S.Should().Be("ORDEM_SERVICO");
        }

        [When(@"o mecânico inclui itens de serviço e peças na Ordem de Serviço")]
        public async Task QuandoOMecanicoIncluiItensDeServicoEPecasNaOrdemDeServico()
        {
            SetAuthHeader(_http);

            // Inclui serviço
            var reqServico = new { ServicosIds = new[] { _context.ServicoId } };
            var resServico = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos", reqServico);
            resServico.StatusCode.Should().Be(HttpStatusCode.OK);

            // Inclui peça
            var reqPeca = new { PecasInsumosIds = new[] { _context.PecaId } };
            var resPeca = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/pecas-insumos", reqPeca);
            resPeca.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [When(@"o mecânico inicia o diagnóstico no OS Service")]
        public async Task QuandoOMecanicoIniciaODiagnosticoNoOsService()
        {
            SetAuthHeader(_http);
            var response = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/iniciar-diagnostico", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"a Ordem de Serviço passa para o status ""(.*)""")]
        public async Task EntaoAOrdemDeServicoPassaParaOStatus(string statusEsperado)
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo(statusEsperado);
        }

        [When(@"o mecânico finaliza o diagnóstico técnico")]
        public async Task QuandoOMecanicoFinalizaODiagnosticoTecnico()
        {
            SetAuthHeader(_http);
            var response = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/finalizar-diagnostico", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"a Ordem de Serviço avança para ""(.*)""")]
        public async Task EntaoAOrdemDeServicoAvancaPara(string statusEsperado)
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo(statusEsperado);
        }

        [When(@"o cliente aprova o orçamento")]
        public async Task QuandoOClienteAprovaOOrcamento()
        {
            SetAuthHeader(_http);
            var response = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/aprovar-orcamento?aprovado=true", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"a Ordem de Serviço é liberada para execução")]
        public async Task EntaoAOrdemDeServicoELiberadaParaExecucao()
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo("Orçamento Aprovado");
        }

        [When(@"o mecânico inicia e finaliza as tarefas de execução")]
        public async Task QuandoOMecanicoIniciaEFinalizaAsTarefasDeExecucao()
        {
            SetAuthHeader(_http);

            // Inicia o serviço
            var resIniciar = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos/{_context.ServicoId}/iniciar", null);
            resIniciar.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Finaliza o serviço
            var resFinalizar = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos/{_context.ServicoId}/finalizar", null);
            resFinalizar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Then(@"a Ordem de Serviço atinge o status ""(.*)""")]
        public async Task EntaoAOrdemDeServicoAtingeOStatus(string statusEsperado)
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo(statusEsperado);
        }

        [When(@"o cliente confirma a entrega do veículo")]
        public async Task QuandoOClienteConfirmaAEntregaDoVeiculo()
        {
            SetAuthHeader(_http);
            var response = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/confirmar-entrega", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"a Ordem de Serviço atinge o status final ""(.*)""")]
        public async Task EntaoAOrdemDeServicoAtingeOStatusFinal(string statusEsperado)
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo(statusEsperado);
        }
    }
}
