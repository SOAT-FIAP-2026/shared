using System.Diagnostics;
using Npgsql;

namespace Fiap.TechChallenge.E2ETests.Support
{
    /// <summary>
    /// Representa o registro persistido da tabela 'orcamento' no PostgreSQL do billing-service.
    /// </summary>
    public record OrcamentoDbRecord(
        Guid Id,
        Guid IdOrdemServico,
        decimal ValorTotal,
        DateTime CriadoEm,
        DateTime AtualizadoEm,
        string? Status = null)
    {
        // Aliases para máxima ergonomia nos cenários de teste e steps BDD
        public Guid OrcamentoId => Id;
        public Guid OsId => IdOrdemServico;
    }

    /// <summary>
    /// Representa o registro persistido da tabela 'pagamento' no PostgreSQL do billing-service.
    /// </summary>
    public record PagamentoDbRecord(
        Guid Id,
        Guid IdOrcamento,
        string? IdMercadoPago,
        string MeioPagamento,
        string Status,
        string? DetalheStatus,
        decimal Valor,
        string ReferenciaExterna,
        string? BoletoUrl,
        string? CodigoBarras,
        DateTimeOffset? DataVencimento,
        DateTime CriadoEm,
        DateTime AtualizadoEm)
    {
        // Aliases para conveniência e compatibilidade com contratos e passos BDD
        public Guid PagamentoId => Id;
        public Guid OrcamentoId => IdOrcamento;
        public string? ExternalPaymentId => IdMercadoPago;
        public string? LinkPagamento => BoletoUrl;
    }

    /// <summary>
    /// Helper de acesso direto ao PostgreSQL do billing-service (techchallenge_billing_db).
    /// Segue o mesmo padrão estabelecido por ExecutionDbHelper, fornecendo queries diretas e polling assíncrono.
    /// </summary>
    public class BillingDbHelper
    {
        private readonly string _connectionString;

        public BillingDbHelper() : this(TestConfiguration.Current) { }

        public BillingDbHelper(TestConfiguration config)
        {
            _connectionString = config.BillingDbConnectionString
                ?? config.Configuration?["ConnectionStrings:BillingDb"]
                ?? "Host=localhost;Port=5434;Database=techchallenge_billing_db;Username=billing;Password=billing_password";
        }

        internal BillingDbHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Consultas de Orçamento

        /// <summary>
        /// Obtém o orçamento associado à Ordem de Serviço informada.
        /// Se informado um timeout > 0, executa polling até que o registro seja encontrado ou o timeout expire.
        /// </summary>
        public async Task<OrcamentoDbRecord?> ObterOrcamentoPorOrdemServicoAsync(
            Guid osId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
            {
                var interval = retryInterval ?? TimeSpan.FromMilliseconds(500);
                return await ExecutarComPollingAsync(
                    () => ObterOrcamentoPorOrdemServicoInternoAsync(osId),
                    timeout.Value,
                    interval);
            }

            return await ObterOrcamentoPorOrdemServicoInternoAsync(osId);
        }

        public async Task<OrcamentoDbRecord?> ObterOrcamentoPorOrdemServicoAsync(
            Guid? osId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!osId.HasValue || osId.Value == Guid.Empty) return null;
            return await ObterOrcamentoPorOrdemServicoAsync(osId.Value, timeout, retryInterval);
        }

        /// <summary>
        /// Obtém o orçamento pelo seu identificador primário.
        /// </summary>
        public async Task<OrcamentoDbRecord?> ObterOrcamentoPorIdAsync(
            Guid orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
            {
                var interval = retryInterval ?? TimeSpan.FromMilliseconds(500);
                return await ExecutarComPollingAsync(
                    () => ObterOrcamentoPorIdInternoAsync(orcamentoId),
                    timeout.Value,
                    interval);
            }

            return await ObterOrcamentoPorIdInternoAsync(orcamentoId);
        }

        public async Task<OrcamentoDbRecord?> ObterOrcamentoPorIdAsync(
            Guid? orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!orcamentoId.HasValue || orcamentoId.Value == Guid.Empty) return null;
            return await ObterOrcamentoPorIdAsync(orcamentoId.Value, timeout, retryInterval);
        }

        /// <summary>
        /// Aguarda até que o orçamento associado à OS seja persistido no banco de dados.
        /// Padrão: 10 segundos de timeout e 500ms de intervalo.
        /// </summary>
        public async Task<OrcamentoDbRecord?> AguardarOrcamentoPorOrdemServicoAsync(
            Guid osId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            var limite = timeout ?? TimeSpan.FromSeconds(10);
            var intervalo = retryInterval ?? TimeSpan.FromMilliseconds(500);

            return await ObterOrcamentoPorOrdemServicoAsync(osId, limite, intervalo);
        }

        public async Task<OrcamentoDbRecord?> AguardarOrcamentoPorOrdemServicoAsync(
            Guid? osId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!osId.HasValue || osId.Value == Guid.Empty) return null;
            return await AguardarOrcamentoPorOrdemServicoAsync(osId.Value, timeout, retryInterval);
        }

        #endregion

        #region Consultas de Pagamento

        /// <summary>
        /// Obtém o pagamento mais recente associado ao orçamento informado.
        /// Se informado timeout > 0, executa polling até encontrar o registro ou atingir o limite.
        /// </summary>
        public async Task<PagamentoDbRecord?> ObterPagamentoPorOrcamentoAsync(
            Guid orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
            {
                var interval = retryInterval ?? TimeSpan.FromMilliseconds(500);
                return await ExecutarComPollingAsync(
                    () => ObterPagamentoPorOrcamentoInternoAsync(orcamentoId),
                    timeout.Value,
                    interval);
            }

            return await ObterPagamentoPorOrcamentoInternoAsync(orcamentoId);
        }

        public async Task<PagamentoDbRecord?> ObterPagamentoPorOrcamentoAsync(
            Guid? orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!orcamentoId.HasValue || orcamentoId.Value == Guid.Empty) return null;
            return await ObterPagamentoPorOrcamentoAsync(orcamentoId.Value, timeout, retryInterval);
        }

        /// <summary>
        /// Obtém o pagamento pelo identificador primário.
        /// </summary>
        public async Task<PagamentoDbRecord?> ObterPagamentoPorIdAsync(
            Guid pagamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
            {
                var interval = retryInterval ?? TimeSpan.FromMilliseconds(500);
                return await ExecutarComPollingAsync(
                    () => ObterPagamentoPorIdInternoAsync(pagamentoId),
                    timeout.Value,
                    interval);
            }

            return await ObterPagamentoPorIdInternoAsync(pagamentoId);
        }

        public async Task<PagamentoDbRecord?> ObterPagamentoPorIdAsync(
            Guid? pagamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!pagamentoId.HasValue || pagamentoId.Value == Guid.Empty) return null;
            return await ObterPagamentoPorIdAsync(pagamentoId.Value, timeout, retryInterval);
        }

        /// <summary>
        /// Obtém o pagamento pelo ID externo do Mercado Pago (id_mercado_pago / ExternalPaymentId).
        /// </summary>
        public async Task<PagamentoDbRecord?> ObterPagamentoPorIdMercadoPagoAsync(
            string idMercadoPago,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
            {
                var interval = retryInterval ?? TimeSpan.FromMilliseconds(500);
                return await ExecutarComPollingAsync(
                    () => ObterPagamentoPorIdMercadoPagoInternoAsync(idMercadoPago),
                    timeout.Value,
                    interval);
            }

            return await ObterPagamentoPorIdMercadoPagoInternoAsync(idMercadoPago);
        }

        /// <summary>
        /// Aguarda até que o pagamento seja criado para o orçamento informado.
        /// Padrão: 10 segundos de timeout e 500ms de intervalo.
        /// </summary>
        public async Task<PagamentoDbRecord?> AguardarPagamentoPorOrcamentoAsync(
            Guid orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            var limite = timeout ?? TimeSpan.FromSeconds(10);
            var intervalo = retryInterval ?? TimeSpan.FromMilliseconds(500);

            return await ObterPagamentoPorOrcamentoAsync(orcamentoId, limite, intervalo);
        }

        public async Task<PagamentoDbRecord?> AguardarPagamentoPorOrcamentoAsync(
            Guid? orcamentoId,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!orcamentoId.HasValue || orcamentoId.Value == Guid.Empty) return null;
            return await AguardarPagamentoPorOrcamentoAsync(orcamentoId.Value, timeout, retryInterval);
        }

        /// <summary>
        /// Aguarda até que o pagamento atinja o status esperado (ex: "Aprovado", "Cancelado").
        /// Padrão: 10 segundos de timeout e 500ms de intervalo.
        /// </summary>
        public async Task<PagamentoDbRecord?> AguardarStatusPagamentoAsync(
            Guid orcamentoId,
            string statusEsperado,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            var limite = timeout ?? TimeSpan.FromSeconds(10);
            var intervalo = retryInterval ?? TimeSpan.FromMilliseconds(500);

            return await ExecutarComPollingAsync(
                () => ObterPagamentoPorOrcamentoInternoAsync(orcamentoId),
                limite,
                intervalo,
                p => string.Equals(p.Status, statusEsperado, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<PagamentoDbRecord?> AguardarStatusPagamentoAsync(
            Guid? orcamentoId,
            string statusEsperado,
            TimeSpan? timeout = null,
            TimeSpan? retryInterval = null)
        {
            if (!orcamentoId.HasValue || orcamentoId.Value == Guid.Empty) return null;
            return await AguardarStatusPagamentoAsync(orcamentoId.Value, statusEsperado, timeout, retryInterval);
        }

        #endregion

        #region Métodos Internos de Execução SQL

        private async Task<OrcamentoDbRecord?> ObterOrcamentoPorOrdemServicoInternoAsync(Guid osId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, id_ordem_servico, valor_total, criado_em, atualizado_em
                FROM orcamento
                WHERE id_ordem_servico = @osId AND apagado_em IS NULL
                ORDER BY criado_em DESC
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("osId", osId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapearOrcamentoRecord(reader);
            }

            return null;
        }

        private async Task<OrcamentoDbRecord?> ObterOrcamentoPorIdInternoAsync(Guid id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, id_ordem_servico, valor_total, criado_em, atualizado_em
                FROM orcamento
                WHERE id = @id AND apagado_em IS NULL
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapearOrcamentoRecord(reader);
            }

            return null;
        }

        private async Task<PagamentoDbRecord?> ObterPagamentoPorOrcamentoInternoAsync(Guid orcamentoId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, id_orcamento, id_mercado_pago, meio_pagamento, status, detalhe_status,
                       valor, referencia_externa, boleto_url, codigo_barras, data_vencimento,
                       criado_em, atualizado_em
                FROM pagamento
                WHERE id_orcamento = @orcamentoId AND apagado_em IS NULL
                ORDER BY criado_em DESC
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("orcamentoId", orcamentoId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapearPagamentoRecord(reader);
            }

            return null;
        }

        private async Task<PagamentoDbRecord?> ObterPagamentoPorIdInternoAsync(Guid id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, id_orcamento, id_mercado_pago, meio_pagamento, status, detalhe_status,
                       valor, referencia_externa, boleto_url, codigo_barras, data_vencimento,
                       criado_em, atualizado_em
                FROM pagamento
                WHERE id = @id AND apagado_em IS NULL
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapearPagamentoRecord(reader);
            }

            return null;
        }

        private async Task<PagamentoDbRecord?> ObterPagamentoPorIdMercadoPagoInternoAsync(string idMercadoPago)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, id_orcamento, id_mercado_pago, meio_pagamento, status, detalhe_status,
                       valor, referencia_externa, boleto_url, codigo_barras, data_vencimento,
                       criado_em, atualizado_em
                FROM pagamento
                WHERE id_mercado_pago = @idMercadoPago AND apagado_em IS NULL
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idMercadoPago", idMercadoPago);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapearPagamentoRecord(reader);
            }

            return null;
        }

        private static OrcamentoDbRecord MapearOrcamentoRecord(NpgsqlDataReader reader)
        {
            return new OrcamentoDbRecord(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetDecimal(2),
                reader.GetDateTime(3),
                reader.GetDateTime(4));
        }

        private static PagamentoDbRecord MapearPagamentoRecord(NpgsqlDataReader reader)
        {
            DateTimeOffset? dataVencimento = null;
            if (!reader.IsDBNull(10))
            {
                try
                {
                    dataVencimento = reader.GetFieldValue<DateTimeOffset>(10);
                }
                catch
                {
                    dataVencimento = new DateTimeOffset(reader.GetDateTime(10));
                }
            }

            return new PagamentoDbRecord(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetDecimal(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                dataVencimento,
                reader.GetDateTime(11),
                reader.GetDateTime(12));
        }

        private static async Task<T?> ExecutarComPollingAsync<T>(
            Func<Task<T?>> queryFunc,
            TimeSpan timeout,
            TimeSpan delay,
            Func<T, bool>? condicaoSucesso = null) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed <= timeout)
            {
                try
                {
                    var resultado = await queryFunc();
                    if (resultado != null && (condicaoSucesso == null || condicaoSucesso(resultado)))
                    {
                        return resultado;
                    }
                }
                catch (NpgsqlException)
                {
                    // Ignora falhas de conexão ou timeouts transitórios durante reinício de contêineres
                }

                if (stopwatch.Elapsed + delay > timeout)
                    break;

                await Task.Delay(delay);
            }

            try
            {
                var finalResult = await queryFunc();
                if (finalResult != null && (condicaoSucesso == null || condicaoSucesso(finalResult)))
                    return finalResult;
            }
            catch
            {
                // Ignora falha na tentativa final
            }

            return null;
        }

        #endregion
    }
}
