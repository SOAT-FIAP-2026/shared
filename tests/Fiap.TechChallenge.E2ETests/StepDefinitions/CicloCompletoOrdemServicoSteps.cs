using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fiap.TechChallenge.Contracts.BillingService;
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
        private readonly BillingDbHelper _billingDb;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private int _estoqueAntesDaBaixa;
        private int _estoqueAposBaixa;

        public CicloCompletoOrdemServicoSteps(
            E2ETestContext context,
            AwsFlociHelper floci,
            ExecutionDbHelper executionDb,
            BillingDbHelper billingDb)
        {
            _context = context;
            _floci = floci;
            _executionDb = executionDb;
            _billingDb = billingDb;
        }

        private string? _mecanicoToken;

        private async Task<string> ObterMecanicoTokenAsync()
        {
            if (!string.IsNullOrEmpty(_mecanicoToken))
                return _mecanicoToken;

            var loginPayload = new { login = "mecanico@techchallenge.com", senha = "Mecanico@123" };
            var response = await _http.PostAsJsonAsync($"{_config.CoreApiUrl}/api/v1/autenticacao/login", loginPayload);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                _mecanicoToken = json.GetProperty("token").GetString();
                return _mecanicoToken!;
            }

            return _context.AdminToken;
        }

        private const string CorrelationHeader = "X-Correlation-ID";
        private readonly string _sagaCorrelationId = $"e2e-saga-{Guid.NewGuid():N}";

        private void SetAuthHeader(HttpClient client, string? customToken = null)
        {
            var token = customToken ?? _context.AdminToken;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            client.DefaultRequestHeaders.Remove(CorrelationHeader);
            client.DefaultRequestHeaders.Add(CorrelationHeader, _sagaCorrelationId);
        }

        [Given(@"que os microsserviços Core, OS e Execução estão operacionais")]
        [Given(@"que os microsserviços Core, OS, Execução e Billing estão operacionais")]
        [Given(@"que os microsserviços Core e OS estão operacionais")]
        public async Task DadoQueOsMicrosservicosEstaoOperacionais()
        {
            var resCore = await _http.GetAsync($"{_config.CoreApiUrl}/health/live");
            resCore.IsSuccessStatusCode.Should().BeTrue("Core Service deve estar operacional");

            var resOs = await _http.GetAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/health");
            resOs.IsSuccessStatusCode.Should().BeTrue("OS Service deve estar operacional");

            var resExec = await _http.GetAsync($"{_config.ExecutionApiUrl}/health/live");
            resExec.IsSuccessStatusCode.Should().BeTrue("Execution Service deve estar operacional");

            var resBilling = await _http.GetAsync($"{_config.BillingApiUrl}/health");
            resBilling.IsSuccessStatusCode.Should().BeTrue("Billing Service deve estar operacional");
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
            var token = await ObterMecanicoTokenAsync();
            SetAuthHeader(_http, token);

            // Inicia diagnóstico no Execution Service (que publica DiagnosticoIniciadoEvent no SNS)
            var response = await _http.PatchAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/iniciar",
                null);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Iniciar diagnóstico no Execution Service deve retornar 200 OK");

            // Aguarda sincronização assíncrona via evento DiagnosticoIniciadoEvent no OS Service (DynamoDB)
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            for (int i = 0; i < 20; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null && item.TryGetValue("statusCodigo", out var sc) && sc.S == "EM_DIAGNOSTICO")
                    break;
                await Task.Delay(500);
            }

            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("EM_DIAGNOSTICO", "OS Service deve atualizar status para EM_DIAGNOSTICO via evento DiagnosticoIniciadoEvent");
        }

        [Then(@"o diagnóstico técnico é gravado no PostgreSQL do Execution Service com status ""(.*)""")]
        public async Task EntaoODiagnosticoTecnicoEGravadoNoPostgreSqlDoExecutionServiceComStatus(string statusEsperado)
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado);

            var diagDb = await _executionDb.ObterDiagnosticoAsync(_context.OrdemProducaoId);
            diagDb.Should().NotBeNull("Registro do diagnóstico deve ser inserido na tabela diagnosticos do PostgreSQL");
            diagDb!.NomeMecanico.Should().Be("Mecanico");
        }

        [When(@"o mecânico finaliza o diagnóstico técnico no Execution Service")]
        public async Task QuandoOMecanicoFinalizaODiagnosticoTecnicoNoExecutionService()
        {
            await _billingDb.GarantirSchemaIsoladoAsync();

            var token = await ObterMecanicoTokenAsync();
            SetAuthHeader(_http, token);

            var request = new
            {
                laudoTecnico = "Diagnóstico técnico validado via E2E BDD",
                servicosSugeridos = new[]
                {
                    new { idServico = _context.ServicoId, descricao = _context.ServicoNome }
                },
                pecasSugeridas = Array.Empty<object>()
            };

            var response = await _http.PostAsJsonAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/finalizar",
                request);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Finalizar diagnóstico deve retornar 200 OK");

            // Aguarda sincronização assíncrona via DiagnosticoTecnicoConcluidoEvent no OS Service (DynamoDB)
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            for (int i = 0; i < 20; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null && item.TryGetValue("statusCodigo", out var sc) && sc.S == "AGUARDANDO_APROVACAO")
                    break;
                await Task.Delay(500);
            }

            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("AGUARDANDO_APROVACAO", "OS Service deve atualizar status para AGUARDANDO_APROVACAO via evento DiagnosticoTecnicoConcluidoEvent");
        }

        [Then(@"o status no PostgreSQL do Execution Service é atualizado para ""(.*)""")]
        public async Task EntaoOStatusNoPostgreSqlDoExecutionServiceEAtualizadoPara(string statusEsperado)
        {
            var ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado);
        }

        [When(@"é gerado o orçamento no Billing Service após o diagnóstico concluído")]
        public async Task QuandoEOrcamentoGeradoNoBillingServiceAposODiagnosticoConcluido()
        {
            await _billingDb.GarantirSchemaIsoladoAsync();

            // Aguarda o BillingSqsBackgroundWorker processar DiagnosticoTecnicoConcluidoEvent (se em voo na fila SQS)
            // para evitar colisão 23505 (IX_orcamento_id_ordem_servico) entre o INSERT do worker e o POST /api/v1/orcamento
            await _billingDb.AguardarOrcamentoPorOrdemServicoAsync(_context.OrdemServicoId, TimeSpan.FromSeconds(5));

            SetAuthHeader(_http);

            var request = new GerarOrcamentoRequest(
                servicos: new[]
                {
                    new ItemCalculoOrcamentoDto(
                        Descricao: _context.ServicoNome,
                        ValorUnitario: 200.00m,
                        Quantidade: 1,
                        TempoEstimadoMinutos: 60,
                        IdItem: _context.ServicoId,
                        Nome: _context.ServicoNome)
                },
                pecasInsumo: new[]
                {
                    new ItemCalculoOrcamentoDto(
                        Descricao: _context.PecaNome,
                        ValorUnitario: 75.50m,
                        Quantidade: 1,
                        IdItem: _context.PecaId,
                        Nome: _context.PecaNome)
                });

            _context.ValorOrcamento = 275.50m;

            HttpResponseMessage? response = null;
            for (int tentativa = 1; tentativa <= 3; tentativa++)
            {
                response = await _http.PostAsJsonAsync(
                    $"{_config.BillingApiUrl}/{BillingApiRoutes.GerarOrcamento}?idOrdemServico={_context.OrdemServicoId}",
                    request);

                _context.UltimaResposta = response;
                _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode || tentativa == 3)
                    break;

                await Task.Delay(300);
            }

            response!.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Geração de orçamento no Billing Service deve retornar 200 OK. Body: {_context.UltimoCorpoResposta}");

            var json = JsonSerializer.Deserialize<JsonElement>(_context.UltimoCorpoResposta!);
            if (json.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var orcId))
            {
                _context.OrcamentoId = orcId;
            }
        }

        [Then(@"o orçamento é gravado no PostgreSQL do Billing Service e a OS aguarda aprovação")]
        public async Task EntaoOOrcamentoEGravadoNoPostgreSqlDoBillingServiceEAOsAguardaAprovacao()
        {
            var orcamentoDb = await _billingDb.AguardarOrcamentoPorOrdemServicoAsync(_context.OrdemServicoId, TimeSpan.FromSeconds(10));
            orcamentoDb.Should().NotBeNull("O orçamento deve ser gravado no PostgreSQL do Billing Service");
            orcamentoDb!.ValorTotal.Should().Be(_context.ValorOrcamento!.Value,
                "O valor total do orçamento gravado no PostgreSQL deve corresponder ao calculado");

            _context.OrcamentoId = orcamentoDb.Id;

            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            decimal valorNoDynamo = 0m;
            for (int i = 0; i < 20; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null &&
                    item.TryGetValue("statusCodigo", out var sc) &&
                    sc.S == "AGUARDANDO_APROVACAO" &&
                    item.TryGetValue("dados", out var dados) &&
                    dados.M != null &&
                    dados.M.TryGetValue("valorOrcamento", out var vo) &&
                    !string.IsNullOrEmpty(vo.N) &&
                    decimal.TryParse(vo.N, NumberStyles.Any, CultureInfo.InvariantCulture, out valorNoDynamo) &&
                    valorNoDynamo == _context.ValorOrcamento!.Value)
                {
                    break;
                }
                await Task.Delay(500);
            }

            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("AGUARDANDO_APROVACAO");
            valorNoDynamo.Should().Be(_context.ValorOrcamento!.Value,
                "O OS Service deve consumir OrcamentoGeradoEvent e atualizar valorOrcamento no DynamoDB");
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

            // Garante que o serviço está vinculado na OS antes da liberação para execução
            var reqServico = new { ServicosIds = new[] { _context.ServicoId } };
            var resServico = await _http.PostAsJsonAsync(
                $"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/servicos",
                reqServico);
            resServico.StatusCode.Should().Be(HttpStatusCode.OK);

            if (_context.OrcamentoId.HasValue && _context.OrcamentoId.Value != Guid.Empty)
            {
                var decisaoUrl = $"{_config.BillingApiUrl}/{string.Format(BillingApiRoutes.DecisaoOrcamento, _context.OrcamentoId.Value)}?{BillingApiRoutes.ParametroAprovado}=true";
                var resDecisao = await _http.GetAsync(decisaoUrl);
                if (resDecisao.IsSuccessStatusCode)
                    return;
            }

            var response = await _http.PatchAsync(
                $"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/aprovar-orcamento?aprovado=true",
                null);
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Aprovação do orçamento deve retornar 200 OK. Resposta da API: {body}");
        }

        [When(@"é realizada a baixa no estoque das peças no Core Service para a Ordem de Serviço")]
        public async Task QuandoERealizadaABaixaNoEstoqueDasPecasNoCoreServiceParaAOrdemDeServico()
        {
            SetAuthHeader(_http);

            _estoqueAntesDaBaixa = await _http.GetFromJsonAsync<int>(
                $"{_config.CoreApiUrl}/api/v1/estoques/quantidade/peca/{_context.PecaId}");

            var reqPeca = new { PecasInsumosIds = new[] { _context.PecaId } };
            var resPeca = await _http.PostAsJsonAsync(
                $"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/pecas-insumos",
                reqPeca);
            var body = await resPeca.Content.ReadAsStringAsync();
            resPeca.StatusCode.Should().Be(HttpStatusCode.OK,
                $"Inclusão de peça na OS para baixa de estoque deve retornar 200 OK. Resposta: {body}");
        }

        [Then(@"o saldo de estoque da peça é reduzido no Core Service")]
        public async Task EntaoOSaldoDeEstoqueDaPecaEReduzidoNoCoreService()
        {
            SetAuthHeader(_http);

            _estoqueAposBaixa = await _http.GetFromJsonAsync<int>(
                $"{_config.CoreApiUrl}/api/v1/estoques/quantidade/peca/{_context.PecaId}");

            _estoqueAposBaixa.Should().Be(_estoqueAntesDaBaixa - 1,
                "A inclusão da peça após aprovação do orçamento deve dar baixa de 1 unidade no estoque do Core Service via ReservaEstoqueSaga");
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
            var token = await ObterMecanicoTokenAsync();
            SetAuthHeader(_http, token);

            var tarefasDb = await _executionDb.ObterTarefasAsync(_context.OrdemProducaoId);
            tarefasDb.Should().NotBeEmpty();

            foreach (var tarefa in tarefasDb)
            {
                // Inicia tarefa no Execution Service
                var resIniciarExec = await _http.PatchAsync(
                    $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/tarefas/{tarefa.Id}/iniciar",
                    null);
                resIniciarExec.StatusCode.Should().Be(HttpStatusCode.OK);

                // Finaliza tarefa no Execution Service (quando a última é concluída, Execution Service publica ProducaoFinalizadaEvent)
                var resFinalizarExec = await _http.PatchAsJsonAsync(
                    $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/tarefas/{tarefa.Id}/finalizar",
                    new { observacoes = "Tarefa executada com perfeição" });
                resFinalizarExec.StatusCode.Should().Be(HttpStatusCode.OK);
            }
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

            string? statusAtual = null;
            for (int i = 0; i < 25; i++)
            {
                var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
                statusAtual = os.GetProperty("statusDescricao").GetString();
                if (string.Equals(statusAtual, statusEsperado, StringComparison.OrdinalIgnoreCase))
                    break;
                await Task.Delay(500);
            }

            statusAtual.Should().BeEquivalentTo(statusEsperado, "OS Service deve atingir o status esperado via consumo assíncrono do ProducaoFinalizadaEvent");
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
