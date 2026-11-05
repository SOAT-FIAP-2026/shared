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
    public class SagaCompensacaoSteps
    {
        private readonly E2ETestContext _context;
        private readonly AwsFlociHelper _floci;
        private readonly BillingDbHelper _billingDb;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private string? _statusAntes;
        private int _estoqueAntesDaReserva;
        private int _estoqueAposReserva;

        public SagaCompensacaoSteps(
            E2ETestContext context,
            AwsFlociHelper floci,
            BillingDbHelper billingDb)
        {
            _context = context;
            _floci = floci;
            _billingDb = billingDb;
        }

        private async Task GarantirAutenticacaoECatalogoAsync()
        {
            if (string.IsNullOrEmpty(_context.AdminToken))
            {
                var loginPayload = new { login = _config.AdminLogin, senha = _config.AdminSenha };
                var response = await _http.PostAsJsonAsync($"{_config.CoreApiUrl}/api/v1/autenticacao/login", loginPayload);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                _context.AdminToken = json.GetProperty("token").GetString();
            }

            SetAuthHeader(_http);

            if (_context.ClienteId == Guid.Empty || _context.VeiculoId == Guid.Empty || _context.PecaId == Guid.Empty)
            {
                var clientes = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/clientes");
                _context.ClienteId = Guid.Parse(clientes![0].GetProperty("id").GetString()!);

                var veiculosDoc = await _http.GetFromJsonAsync<JsonDocument>($"{_config.CoreApiUrl}/api/v1/veiculos");
                var veiculosRoot = veiculosDoc!.RootElement;
                JsonElement veiculo = veiculosRoot.ValueKind == JsonValueKind.Array
                    ? veiculosRoot[0]
                    : veiculosRoot.GetProperty("items")[0];
                _context.VeiculoId = Guid.Parse(veiculo.GetProperty("id").GetString()!);

                var servicos = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/servicos");
                _context.ServicoId = Guid.Parse(servicos![0].GetProperty("id").GetString()!);
                _context.ServicoNome = servicos[0].GetProperty("nome").GetString()!;

                var pecas = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/pecas-insumos");
                _context.PecaId = Guid.Parse(pecas![0].GetProperty("id").GetString()!);
                _context.PecaNome = pecas[0].GetProperty("descricao").GetString()!;
            }
        }

        private void SetAuthHeader(HttpClient client)
        {
            if (!string.IsNullOrEmpty(_context.AdminToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _context.AdminToken);
            }
        }

        [Given(@"que o atendente possui uma Ordem de Serviço aberta")]
        public async Task DadoQueOAtendentePossuiUmaOrdemDeServicoAberta()
        {
            await GarantirAutenticacaoECatalogoAsync();

            var request = new
            {
                clienteId = _context.ClienteId,
                veiculoId = _context.VeiculoId,
                observacao = "OS para teste de compensação e resiliência"
            };

            var response = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico", request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            _context.OrdemServicoId = Guid.Parse(json.GetProperty("id").GetString()!);

            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            _statusAntes = os.GetProperty("statusDescricao").GetString();
        }

        [When(@"o atendente tenta incluir uma peça com identificador inexistente")]
        public async Task QuandoOAtendenteTentaIncluirUmaPecaComIdentificadorInexistente()
        {
            SetAuthHeader(_http);

            var pecaInexistenteId = Guid.NewGuid();
            var reqPeca = new { PecasInsumosIds = new[] { pecaInexistenteId } };

            var response = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/pecas-insumos", reqPeca);
            _context.UltimaResposta = response;
            _context.UltimoCorpoResposta = await response.Content.ReadAsStringAsync();
        }

        [Then(@"o sistema rejeita a operação com erro de validação")]
        public void EntaoOSistemaRejeitaAOperacaoComErroDeValidacao()
        {
            _context.UltimaResposta.Should().NotBeNull();
            ((int)_context.UltimaResposta!.StatusCode).Should().BeInRange(400, 499, "A operação deve ser rejeitada com código 4xx");
        }

        [Then(@"o status da Ordem de Serviço permanece inalterado")]
        public async Task EntaoOStatusDaOrdemDeServicoPermaneceInalterado()
        {
            SetAuthHeader(_http);
            var os = await _http.GetFromJsonAsync<JsonElement>($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}");
            var statusDepois = os.GetProperty("statusDescricao").GetString();

            statusDepois.Should().Be(_statusAntes, "O status da OS não deve ser modificado após uma operação inválida");
        }

        [Given(@"que o atendente possui uma Ordem de Serviço aguardando pagamento com peças reservadas no estoque")]
        public async Task DadoQueOAtendentePossuiUmaOrdemDeServicoAguardandoPagamentoComPecasReservadasNoEstoque()
        {
            await GarantirAutenticacaoECatalogoAsync();

            // 1. Consulta estoque inicial da peça no Core Service
            _estoqueAntesDaReserva = await _http.GetFromJsonAsync<int>(
                $"{_config.CoreApiUrl}/api/v1/estoques/quantidade/peca/{_context.PecaId}");

            // 2. Abre nova Ordem de Serviço no OS Service
            var abrirRequest = new
            {
                clienteId = _context.ClienteId,
                veiculoId = _context.VeiculoId,
                observacao = "OS para SAGA Compensação Assíncrona de Pagamento Recusado"
            };

            var resCriar = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico", abrirRequest);
            resCriar.StatusCode.Should().Be(HttpStatusCode.Created);

            var osJson = await resCriar.Content.ReadFromJsonAsync<JsonElement>();
            _context.OrdemServicoId = Guid.Parse(osJson.GetProperty("id").GetString()!);

            // 3. Inclui peça na OS (aciona ReservaEstoqueSaga e baixa 1 unidade no Core Service)
            var reqPeca = new { PecasInsumosIds = new[] { _context.PecaId } };
            var resPeca = await _http.PostAsJsonAsync($"{_config.OsApiUrl}/api/v1/ordens-servico/{_context.OrdemServicoId}/pecas-insumos", reqPeca);
            resPeca.StatusCode.Should().Be(HttpStatusCode.OK);

            _estoqueAposReserva = await _http.GetFromJsonAsync<int>(
                $"{_config.CoreApiUrl}/api/v1/estoques/quantidade/peca/{_context.PecaId}");
            _estoqueAposReserva.Should().Be(_estoqueAntesDaReserva - 1,
                "A inclusão da peça na OS deve reservar 1 unidade do estoque no Core Service");

            // 4. Gera orçamento e boleto no Billing Service para colocar a OS aguardando pagamento
            var orcamentoRequest = new GerarOrcamentoRequest(
                servicos: new[]
                {
                    new ItemCalculoOrcamentoDto(_context.ServicoNome, 150m, 1, 60, _context.ServicoId, _context.ServicoNome)
                },
                pecasInsumo: new[]
                {
                    new ItemCalculoOrcamentoDto(_context.PecaNome, 50m, 1, null, _context.PecaId, _context.PecaNome)
                });

            var resOrc = await _http.PostAsJsonAsync(
                $"{_config.BillingApiUrl}/{BillingApiRoutes.GerarOrcamento}?idOrdemServico={_context.OrdemServicoId}",
                orcamentoRequest);
            resOrc.StatusCode.Should().Be(HttpStatusCode.OK);

            var orcamentoDb = await _billingDb.AguardarOrcamentoPorOrdemServicoAsync(_context.OrdemServicoId, TimeSpan.FromSeconds(10));
            orcamentoDb.Should().NotBeNull();
            _context.OrcamentoId = orcamentoDb!.Id;

            var boletoRequest = new CriarBoletoRequest(
                IdOrcamento: _context.OrcamentoId.Value,
                Email: "cliente@techchallenge.com",
                Nome: "Cliente",
                Sobrenome: "Saga",
                Cpf: "12345678909");

            var resBoleto = await _http.PostAsJsonAsync($"{_config.BillingApiUrl}/{BillingApiRoutes.CriarBoleto}", boletoRequest);
            resBoleto.StatusCode.Should().Be(HttpStatusCode.Created);

            var boleto = await resBoleto.Content.ReadFromJsonAsync<BoletoResponse>();
            boleto.Should().NotBeNull();
            _context.PagamentoId = boleto!.PagamentoId;
            _context.ExternalPaymentId = boleto.PaymentId;
        }

        [When(@"o pagamento é recusado no Billing Service disparando PagamentoCanceladoCompensacaoEvent no SNS")]
        public async Task QuandoOPagamentoERecusadoNoBillingServiceDisparandoPagamentoCanceladoCompensacaoEventNoSns()
        {
            _context.ExternalPaymentId.Should().NotBeNullOrWhiteSpace();

            var webhookPayload = new
            {
                action = "payment.updated",
                type = "payment",
                status = "rejected",
                data = new
                {
                    id = _context.ExternalPaymentId,
                    status = "rejected"
                }
            };

            var response = await _http.PostAsJsonAsync(
                $"{_config.BillingApiUrl}/{BillingApiRoutes.WebhookMercadoPago}",
                webhookPayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK, "Webhook de recusa de pagamento deve retornar 200 OK");

            var pagamentoDb = await _billingDb.AguardarStatusPagamentoAsync(_context.OrcamentoId, "Rejeitado", TimeSpan.FromSeconds(10));
            pagamentoDb.Should().NotBeNull("O status do pagamento no Billing Service deve ser atualizado para Rejeitado");
        }

        [Then(@"a Lambda de compensação do OS Service consome o evento da fila SQS")]
        public async Task EntaoALambdaDeCompensacaoDoOsServiceConsomeOEventoDaFilaSqs()
        {
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>? item = null;
            for (int i = 0; i < 25; i++)
            {
                item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
                if (item != null && item.TryGetValue("statusCodigo", out var sc) && sc.S == "CANCELADA")
                {
                    break;
                }
                await Task.Delay(500);
            }

            item.Should().NotBeNull("O worker de compensação do OS Service deve processar o evento da fila SQS");
            item!["statusCodigo"].S.Should().Be("CANCELADA");
        }

        [Then(@"o estoque da peça reservada é estornado no Core Service")]
        public async Task EntaoOEstoqueDaPecaReservadaEEstornadoNoCoreService()
        {
            SetAuthHeader(_http);

            int estoqueAtual = _estoqueAposReserva;
            for (int i = 0; i < 20; i++)
            {
                estoqueAtual = await _http.GetFromJsonAsync<int>(
                    $"{_config.CoreApiUrl}/api/v1/estoques/quantidade/peca/{_context.PecaId}");
                if (estoqueAtual == _estoqueAntesDaReserva)
                    break;
                await Task.Delay(500);
            }

            estoqueAtual.Should().Be(_estoqueAntesDaReserva,
                "A compensação da SAGA deve devolver a peça reservada ao estoque do Core Service");
        }

        [Then(@"o status da Ordem de Serviço no DynamoDB é atualizado para ""(.*)""")]
        public async Task EntaoOStatusDaOrdemDeServicoNoDynamoDbEAtualizadoPara(string statusEsperado)
        {
            var item = await _floci.ObterOrdemServicoDynamo(_context.OrdemServicoId);
            item.Should().NotBeNull();
            item!["statusCodigo"].S.Should().Be(statusEsperado);
        }
    }
}
