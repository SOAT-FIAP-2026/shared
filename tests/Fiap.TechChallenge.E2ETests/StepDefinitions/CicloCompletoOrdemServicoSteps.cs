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
        private readonly ExecutionDbHelper _executionDb;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public CicloCompletoOrdemServicoSteps(
            E2ETestContext context,
            AwsFlociHelper floci,
            ExecutionDbHelper executionDb)
        {
            _context = context;
            _floci = floci;
            _executionDb = executionDb;
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
            _context.VeiculoModelo = veiculo.TryGetProperty("modelo", out var m) ? m.GetString() ?? "Veiculo" : "Veiculo";
            _context.VeiculoAno = veiculo.TryGetProperty("ano", out var a) ? a.GetInt32() : 2022;

            // Obtém primeiro serviço e primeira peça do catálogo
            var servicos = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/servicos");
            servicos.Should().NotBeNullOrEmpty("Deve haver serviços cadastrados");
            _context.ServicoId = Guid.Parse(servicos![0].GetProperty("id").GetString()!);
            _context.ServicoNome = servicos[0].GetProperty("nome").GetString()!;

            var pecas = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/pecas-insumos");
            pecas.Should().NotBeNullOrEmpty("Deve haver peças cadastradas");
            _context.PecaId = Guid.Parse(pecas![0].GetProperty("id").GetString()!);
            _context.PecaNome = pecas[0].GetProperty("descricao").GetString()!;
        }

        [When(@"o atendente solicita a abertura de uma nova Ordem de Serviço no OS Service")]
        public async Task QuandoOAtendenteSolicitaAAberturaDeUmaNovaOrdemDeServicoNoOsService()
        {
            SetAuthHeader(_http);

            var request = new
            {
                clienteId = _context.ClienteId,
                veiculoId = _context.VeiculoId,
                observacao = "Abertura E2E BDD Test Integrado"
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

        [Then(@"a Ordem de Serviço é gravada fisicamente no DynamoDB")]
        [Then(@"a Ordem de Serviço pode ser consultada no DynamoDB")]
        public async Task EntaoAOrdemDeServicoEGravadaFisicamenteNoDynamoDB()
        {
            var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
            item.Should().NotBeNull("Item correspondente à OS deve existir na tabela DynamoDB");
            item!["PK"].S.Should().Be($"OS#{_context.OrdemServicoId}");
            item["statusCodigo"].S.Should().Be("RECEBIDA");
            item["tipoEntidade"].S.Should().Be("ORDEM_SERVICO");
        }

        [Then(@"o evento de abertura da OS é integrado com o Execution Service")]
        public async Task EntaoOEventoDeAberturaDaOSEIntegradoComOExecutionService()
        {
            // Aguarda o worker SQS do execution-service consumir o evento publicado pelo os-microservice no SNS
            OrdemProducaoDbRecord? ordemDb = null;
            for (int i = 0; i < 15; i++)
            {
                ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
                if (ordemDb != null) break;
                await Task.Delay(500);
            }

            ordemDb.Should().NotBeNull("Execution Service deve consumir OrdemServicoAbertaEvent da fila SQS e criar OrdemProducao");
        }

        [Then(@"a Ordem de Produção é gravada no PostgreSQL do Execution Service com status ""(.*)""")]
        public async Task EntaoAOrdemDeProducaoEGravadaNoPostgreSqlDoExecutionServiceComStatus(string statusEsperado)
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull("OrdemProducao deve ser gravada no banco PostgreSQL do Execution Service");
            ordemDb!.Status.Should().Be(statusEsperado, "Status no banco PostgreSQL deve coincidir");
            ordemDb.ClienteNome.Should().Be(_context.ClienteNome);
            ordemDb.VeiculoPlaca.Should().Be(_context.VeiculoPlaca);

            _context.OrdemProducaoId = ordemDb.Id;
        }

        [When(@"o mecânico inicia o diagnóstico no Execution Service")]
        public async Task QuandoOMecanicoIniciaODiagnosticoNoExecutionService()
        {
            SetAuthHeader(_http);

            // Inicia diagnóstico no Execution Service
            var response = await _http.PatchAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/iniciar",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Iniciar diagnóstico no Execution Service deve retornar 200 OK");

            // Espelha o status no OS Service (EM_DIAGNOSTICO)
            var resOs = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/iniciar-diagnostico", null);
            resOs.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"o diagnóstico técnico é gravado no PostgreSQL do Execution Service com status ""(.*)""")]
        public async Task EntaoODiagnosticoTecnicoEGravadoNoPostgreSqlDoExecutionServiceComStatus(string statusEsperado)
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado);

            var diagDb = await _executionDb.ObterDiagnosticoAsync(_context.OrdemProducaoId);
            diagDb.Should().NotBeNull("Registro do diagnóstico deve ser inserido na tabela diagnosticos do PostgreSQL");
            diagDb!.NomeMecanico.Should().Be("Administrador");
        }

        [When(@"o mecânico finaliza o diagnóstico técnico no Execution Service")]
        public async Task QuandoOMecanicoFinalizaODiagnosticoTecnicoNoExecutionService()
        {
            SetAuthHeader(_http);

            var request = new
            {
                laudoTecnico = "Diagnóstico técnico validado via E2E BDD",
                servicosSugeridos = new[]
                {
                    new { idServico = _context.ServicoId, descricao = _context.ServicoNome }
                },
                pecasSugeridas = new[]
                {
                    new { idPeca = _context.PecaId, quantidade = 1, descricao = _context.PecaNome }
                }
            };

            var response = await _http.PostAsJsonAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/finalizar",
                request);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Finalizar diagnóstico deve retornar 200 OK");

            // Finaliza o diagnóstico no OS Service para avançar a OS para AguardandoAprovacao
            var resOs = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/finalizar-diagnostico", null);
            resOs.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Then(@"o status no PostgreSQL do Execution Service é atualizado para ""(.*)""")]
        public async Task EntaoOStatusNoPostgreSqlDoExecutionServiceEAtualizadoPara(string statusEsperado)
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado);
        }

        [When(@"o mecânico inclui itens de serviço e peças na Ordem de Serviço")]
        public async Task QuandoOMecanicoIncluiItensDeServicoEPecasNaOrdemDeServico()
        {
            SetAuthHeader(_http);

            // Inclui serviço no OS Service
            var reqServico = new { ServicosIds = new[] { _context.ServicoId } };
            var resServico = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos", reqServico);
            resServico.StatusCode.Should().Be(HttpStatusCode.OK);

            // Inclui peça no OS Service
            var reqPeca = new { PecasInsumosIds = new[] { _context.PecaId } };
            var resPeca = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/pecas-insumos", reqPeca);
            resPeca.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [When(@"o cliente aprova o orçamento")]
        public async Task QuandoOClienteAprovaOOrcamento()
        {
            SetAuthHeader(_http);
            var response = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/aprovar-orcamento?aprovado=true", null);
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Aprovação do orçamento deve retornar 200 OK. Resposta da API: {body}");
        }

        [Then(@"a Ordem de Serviço é atualizada no DynamoDB com orçamento aprovado")]
        public async Task EntaoAOrdemDeServicoEAtualizadaNoDynamoDBComOrcamentoAprovado()
        {
            var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("ORCAMENTO_APROVADO", "Status no DynamoDB deve refletir ORCAMENTO_APROVADO");
        }

        [Then(@"o Execution Service recebe a liberação e grava as tarefas no PostgreSQL com status ""(.*)""")]
        public async Task EntaoOExecutionServiceRecebeALiberacaoEGravaAsTarefasNoPostgreSqlComStatus(string statusEsperado)
        {
            // Aguarda o worker SQS do execution-service consumir o evento OrdemServicoLiberadaParaExecucaoEvent
            OrdemProducaoDbRecord? ordemDb = null;
            for (int i = 0; i < 15; i++)
            {
                ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
                if (ordemDb != null && ordemDb.Status == statusEsperado) break;
                await Task.Delay(500);
            }

            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado, "OrdemProducao deve avançar para status NA_FILA via evento SQS");

            var tarefasDb = await _executionDb.ObterTarefasAsync(_context.OrdemProducaoId);
            tarefasDb.Should().NotBeEmpty("Tarefas de produção devem ser gravadas na tabela tarefas_producao via evento SQS");
            _context.TarefaProducaoId = tarefasDb[0].Id;
        }

        [When(@"o mecânico inicia e finaliza as tarefas de execução no Execution Service")]
        public async Task QuandoOMecanicoIniciaEFinalizaAsTarefasDeExecucaoNoExecutionService()
        {
            SetAuthHeader(_http);

            // Inicia tarefa no Execution Service
            var resIniciarExec = await _http.PatchAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/tarefas/{_context.TarefaProducaoId}/iniciar",
                null);
            resIniciarExec.StatusCode.Should().Be(HttpStatusCode.OK);

            // Finaliza tarefa no Execution Service
            var resFinalizarExec = await _http.PatchAsJsonAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/tarefas/{_context.TarefaProducaoId}/finalizar",
                new { observacoes = "Tarefa executada com perfeição" });
            resFinalizarExec.StatusCode.Should().Be(HttpStatusCode.OK);

            // Atualiza OS Service em paralelo para manter o espelhamento
            var resIniciarOs = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos/{_context.ServicoId}/iniciar", null);
            resIniciarOs.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var resFinalizarOs = await _http.PatchAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos/{_context.ServicoId}/finalizar", null);
            resFinalizarOs.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Then(@"a Ordem de Produção é concluída no PostgreSQL do Execution Service")]
        public async Task EntaoAOrdemDeProducaoEConcluidaNoPostgreSqlDoExecutionService()
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be("CONCLUIDA", "OrdemProducao deve estar CONCLUIDA no PostgreSQL do Execution Service");

            var tarefasDb = await _executionDb.ObterTarefasAsync(_context.OrdemProducaoId);
            tarefasDb.Should().OnlyContain(t => t.Status == "CONCLUIDA", "Todas as tarefas no PostgreSQL devem estar CONCLUIDA");
        }

        [Then(@"a Ordem de Serviço no OS Service atinge o status ""(.*)""")]
        public async Task EntaoAOrdemDeServicoNoOsServiceAtingeOStatus(string statusEsperado)
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

        [Then(@"a Ordem de Serviço atinge o status final ""(.*)"" no DynamoDB")]
        public async Task EntaoAOrdemDeServicoAtingeOStatusFinalNoDynamoDB(string statusEsperado)
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            os.GetProperty("statusDescricao").GetString().Should().BeEquivalentTo(statusEsperado);

            var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("ENTREGUE", "Registro físico no DynamoDB deve estar com statusCodigo = ENTREGUE");
        }
    }
}
