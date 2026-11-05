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
    public class FaturamentoPagamentoSteps
    {
        private readonly E2ETestContext _context;
        private readonly AwsFlociHelper _floci;
        private readonly ExecutionDbHelper _executionDb;
        private readonly BillingDbHelper _billingDb;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public FaturamentoPagamentoSteps(
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

        private void SetAuthHeader(HttpClient client, string? customToken = null)
        {
            var token = customToken ?? _context.AdminToken;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task<string?> ObterMecanicoTokenAsync()
        {
            var loginPayload = new { login = "mecanico@techchallenge.com", senha = "Mecanico@123" };
            var response = await _http.PostAsJsonAsync($"{_config.CoreApiUrl}/api/v1/autenticacao/login", loginPayload);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                return json.GetProperty("token").GetString();
            }

            return _context.AdminToken;
        }

        [Given(@"que existe uma Ordem de Serviço em fase de diagnóstico concluído com serviços e peças")]
        public async Task DadoQueExisteUmaOrdemDeServicoEmFaseDeDiagnosticoConcluidoComServicosEPecas()
        {
            SetAuthHeader(_http);

            // 1. Abre nova Ordem de Serviço no OS Service
            var abrirRequest = new
            {
                clienteId = _context.ClienteId,
                veiculoId = _context.VeiculoId,
                observacao = "OS para E2E Faturamento e Pagamento (Billing Service)"
            };

            var resCriar = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico", abrirRequest);
            resCriar.StatusCode.Should().Be(HttpStatusCode.Created);

            var osJson = await resCriar.Content.ReadFromJsonAsync<JsonElement>();
            _context.OrdemServicoId = Guid.Parse(osJson.GetProperty("id").GetString()!);

            // 2. Aguarda OrdemProducao no Execution Service via SQS
            OrdemProducaoDbRecord? ordemDb = null;
            for (int i = 0; i < 20; i++)
            {
                ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
                if (ordemDb != null) break;
                await Task.Delay(500);
            }

            ordemDb.Should().NotBeNull("OrdemProducao deve ser criada no Execution Service via OrdemServicoAbertaEvent");
            _context.OrdemProducaoId = ordemDb!.Id;

            // 3. Inicia e finaliza diagnóstico no Execution Service
            var mecanicoToken = await ObterMecanicoTokenAsync();
            SetAuthHeader(_http, mecanicoToken);

            var resIniciarDiag = await _http.PatchAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/iniciar",
                null);
            resIniciarDiag.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalizarDiagRequest = new
            {
                laudoTecnico = "Diagnóstico concluído para geração de orçamento e pagamento",
                servicosSugeridos = new[]
                {
                    new { idServico = _context.ServicoId, descricao = _context.ServicoNome }
                },
                pecasSugeridas = new[]
                {
                    new { idPeca = _context.PecaId, quantidade = 1, descricao = _context.PecaNome }
                }
            };

            var resFinalizarDiag = await _http.PostAsJsonAsync(
                $"{_config.ExecutionApiUrl}/api/v1/producao/{_context.OrdemProducaoId}/diagnostico/finalizar",
                finalizarDiagRequest);
            resFinalizarDiag.StatusCode.Should().Be(HttpStatusCode.OK);

            // 4. Aguarda OS atingir AGUARDANDO_APROVACAO via evento DiagnosticoTecnicoConcluidoEvent
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            for (int i = 0; i < 20; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null && item.TryGetValue("statusCodigo", out var sc) && sc.S == "AGUARDANDO_APROVACAO")
                    break;
                await Task.Delay(500);
            }

            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be("AGUARDANDO_APROVACAO");
        }

        [When(@"o orçamento é solicitado no Billing Service utilizando o contrato compartilhado GerarOrcamentoRequest")]
        public async Task QuandoOOrcamentoESolicitadoNoBillingServiceUtilizandoOContratoCompartilhadoGerarOrcamentoRequest()
        {
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
                        Quantidade: 2,
                        IdItem: _context.PecaId,
                        Nome: _context.PecaNome)
                });

            _context.ValorOrcamento = 200.00m + (75.50m * 2); // 351.00m

            var response = await _http.PostAsJsonAsync(
                $"{_config.BillingApiUrl}/{BillingApiRoutes.GerarOrcamento}?idOrdemServico={_context.OrdemServicoId}",
                request);

            _context.UltimaResposta = response;
            _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Geração de orçamento no Billing Service deve retornar 200 OK. Body: {_context.UltimoCorpoResposta}");

            var json = JsonSerializer.Deserialize<JsonElement>(_context.UltimoCorpoResposta);
            if (json.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var orcId))
            {
                _context.OrcamentoId = orcId;
            }
        }

        [Then(@"o orçamento é gravado no PostgreSQL do Billing Service com o valor total calculado corretamente")]
        public async Task EntaoOOrcamentoEGravadoNoPostgreSqlDoBillingServiceComOValorTotalCalculadoCorretamente()
        {
            var orcamentoDb = await _billingDb.AguardarOrcamentoPorOrdemServicoAsync(_context.OrdemServicoId, TimeSpan.FromSeconds(10));
            orcamentoDb.Should().NotBeNull("O orçamento deve estar persistido na tabela orcamento do PostgreSQL do Billing Service");
            orcamentoDb!.IdOrdemServico.Should().Be(_context.OrdemServicoId);
            orcamentoDb.ValorTotal.Should().Be(_context.ValorOrcamento!.Value, "O valor total calculado no banco deve corresponder à soma de serviços e peças");

            _context.OrcamentoId = orcamentoDb.Id;
        }

        [Then(@"o evento OrcamentoGeradoEvent é publicado no SNS com os atributos canônicos")]
        public async Task EntaoOEventoOrcamentoGeradoEventEPublicadoNoSnsComOsAtributosCanonicos()
        {
            // Quando o OrcamentoGeradoEvent é publicado no SNS com tipoEvento="OrcamentoGeradoEvent",
            // a FilterPolicy da fila techchallenge-os-eventos-local roteia a mensagem para o OS Service,
            // que atualiza o campo valorOrcamento dentro do mapa "dados" no DynamoDB para o valor calculado pelo Billing Service.
            decimal valorNoDynamo = 0m;
            for (int i = 0; i < 20; i++)
            {
                var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null &&
                    item.TryGetValue("dados", out var dados) &&
                    dados.M != null &&
                    dados.M.TryGetValue("valorOrcamento", out var vo) &&
                    !string.IsNullOrEmpty(vo.N))
                {
                    if (decimal.TryParse(vo.N, NumberStyles.Any, CultureInfo.InvariantCulture, out valorNoDynamo) &&
                        valorNoDynamo == _context.ValorOrcamento!.Value)
                    {
                        break;
                    }
                }
                await Task.Delay(500);
            }

            valorNoDynamo.Should().Be(_context.ValorOrcamento!.Value,
                "O OS Service deve consumir OrcamentoGeradoEvent (filtrado via atributos canônicos SNS) e atualizar valorOrcamento no DynamoDB");
        }

        [When(@"o cliente solicita a emissão de boleto no Billing Service via CriarBoletoRequest")]
        public async Task QuandoOClienteSolicitaAEmissaoDeBoletoNoBillingServiceViaCriarBoletoRequest()
        {
            SetAuthHeader(_http);

            _context.OrcamentoId.Should().NotBeNull("OrcamentoId deve estar preenchido antes de criar o boleto");

            var boletoRequest = new CriarBoletoRequest(
                IdOrcamento: _context.OrcamentoId!.Value,
                Email: string.IsNullOrWhiteSpace(_context.ClienteEmail) ? "cliente@techchallenge.com" : _context.ClienteEmail,
                Nome: "Cliente",
                Sobrenome: "TechChallenge",
                Cpf: "12345678909");

            var response = await _http.PostAsJsonAsync(
                $"{_config.BillingApiUrl}/{BillingApiRoutes.CriarBoleto}",
                boletoRequest);

            _context.UltimaResposta = response;
            _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.Created, $"Criação de boleto deve retornar 201 Created. Body: {_context.UltimoCorpoResposta}");

            var boletoResponse = await response.Content.ReadFromJsonAsync<BoletoResponse>();
            boletoResponse.Should().NotBeNull();
            boletoResponse!.PagamentoId.Should().NotBeEmpty();
            boletoResponse.PaymentId.Should().NotBeNullOrWhiteSpace();

            _context.PagamentoId = boletoResponse.PagamentoId;
            _context.ExternalPaymentId = boletoResponse.PaymentId;
        }

        [Then(@"o pagamento é registrado com status ""(.*)"" no PostgreSQL do Billing Service")]
        public async Task EntaoOPagamentoERegistradoComStatusNoPostgreSqlDoBillingService(string statusEsperado)
        {
            var pagamentoDb = await _billingDb.AguardarPagamentoPorOrcamentoAsync(_context.OrcamentoId, TimeSpan.FromSeconds(10));
            pagamentoDb.Should().NotBeNull("O pagamento deve estar persistido na tabela pagamento do PostgreSQL do Billing Service");
            pagamentoDb!.IdOrcamento.Should().Be(_context.OrcamentoId!.Value);
            pagamentoDb.Status.Should().BeEquivalentTo(statusEsperado);
            pagamentoDb.IdMercadoPago.Should().Be(_context.ExternalPaymentId);
            pagamentoDb.BoletoUrl.Should().NotBeNullOrWhiteSpace();
            pagamentoDb.CodigoBarras.Should().NotBeNullOrWhiteSpace();
        }

        [When(@"o Billing Service recebe a notificação de pagamento aprovado no endpoint de Webhook")]
        public async Task QuandoOBillingServiceRecebeANotificacaoDePagamentoAprovadoNoEndpointDeWebhook()
        {
            _context.ExternalPaymentId.Should().NotBeNullOrWhiteSpace("ExternalPaymentId (MercadoPago ID) deve estar disponível");

            var webhookPayload = new
            {
                action = "payment.updated",
                type = "payment",
                status = "approved",
                data = new
                {
                    id = _context.ExternalPaymentId,
                    status = "approved"
                }
            };

            var response = await _http.PostAsJsonAsync(
                $"{_config.BillingApiUrl}/{BillingApiRoutes.WebhookMercadoPago}",
                webhookPayload);

            _context.UltimaResposta = response;
            _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Webhook do Mercado Pago no Billing Service deve responder 200 OK");
        }

        [Then(@"o status do pagamento no PostgreSQL do Billing Service é atualizado para ""(.*)""")]
        public async Task EntaoOStatusDoPagamentoNoPostgreSqlDoBillingServiceEAtualizadoPara(string statusEsperado)
        {
            var pagamentoDb = await _billingDb.AguardarStatusPagamentoAsync(_context.OrcamentoId, statusEsperado, TimeSpan.FromSeconds(10));
            pagamentoDb.Should().NotBeNull($"O pagamento no PostgreSQL do Billing Service deve atingir o status {statusEsperado}");
            pagamentoDb!.Status.Should().BeEquivalentTo(statusEsperado);
        }

        [Then(@"o evento PagamentoConfirmadoEvent é consumido pelo OS Service atualizando a OS no DynamoDB para ""(.*)""")]
        public async Task EntaoOEventoPagamentoConfirmadoEventEConsumidoPeloOsServiceAtualizandoAOsNoDynamoDbPara(string statusEsperado)
        {
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            for (int i = 0; i < 25; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null && item.TryGetValue("statusCodigo", out var sc) &&
                    (sc.S == statusEsperado || sc.S == "EM_EXECUCAO"))
                {
                    break;
                }
                await Task.Delay(500);
            }

            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be(statusEsperado,
                $"O OS Service deve consumir PagamentoConfirmadoEvent da fila SQS e atualizar statusCodigo no DynamoDB para {statusEsperado}");
        }

        [Then(@"o Execution Service recebe automaticamente a liberação via SQS gravando as tarefas com status ""(.*)""")]
        public async Task EntaoOExecutionServiceRecebeAutomaticamenteALiberacaoViaSqsGravandoAsTarefasComStatus(string statusEsperado)
        {
            OrdemProducaoDbRecord? ordemDb = null;
            for (int i = 0; i < 25; i++)
            {
                ordemDb = await _executionDb.ObterOrdemProducaoAsync(_context.OrdemServicoId);
                if (ordemDb != null && ordemDb.Status == statusEsperado)
                    break;
                await Task.Delay(500);
            }

            ordemDb.Should().NotBeNull();
            ordemDb!.Status.Should().Be(statusEsperado,
                "Execution Service deve consumir OrdemServicoLiberadaParaExecucaoEvent (publicado pelo OS Service ao receber PagamentoConfirmadoEvent) e avançar OrdemProducao para NA_FILA");

            var tarefasDb = await _executionDb.ObterTarefasAsync(_context.OrdemProducaoId);
            tarefasDb.Should().NotBeEmpty("As tarefas de execução devem ser criadas no PostgreSQL do Execution Service");
        }
    }
}
