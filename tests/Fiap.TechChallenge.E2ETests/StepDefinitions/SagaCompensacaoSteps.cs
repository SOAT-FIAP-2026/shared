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
    public class SagaCompensacaoSteps
    {
        private readonly E2ETestContext _context;
        private readonly TestConfiguration _config = TestConfiguration.Current;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private string? _statusAntes;

        public SagaCompensacaoSteps(E2ETestContext context)
        {
            _context = context;
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
            SetAuthHeader(_http);

            if (_context.ClienteId == Guid.Empty || _context.VeiculoId == Guid.Empty)
            {
                var clientes = await _http.GetFromJsonAsync<List<JsonElement>>($"{_config.CoreApiUrl}/api/v1/clientes");
                _context.ClienteId = Guid.Parse(clientes![0].GetProperty("id").GetString()!);

                var veiculosDoc = await _http.GetFromJsonAsync<JsonDocument>($"{_config.CoreApiUrl}/api/v1/veiculos");
                var veiculosRoot = veiculosDoc!.RootElement;
                JsonElement veiculo = veiculosRoot.ValueKind == JsonValueKind.Array
                    ? veiculosRoot[0]
                    : veiculosRoot.GetProperty("items")[0];
                _context.VeiculoId = Guid.Parse(veiculo.GetProperty("id").GetString()!);
            }

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
    }
}
