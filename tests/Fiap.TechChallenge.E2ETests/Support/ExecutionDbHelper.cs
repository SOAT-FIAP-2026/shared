using Npgsql;

namespace Fiap.TechChallenge.E2ETests.Support
{
    public record OrdemProducaoDbRecord(
        Guid Id,
        Guid OrdemServicoId,
        string Status,
        int Versao,
        string ClienteNome,
        string VeiculoPlaca);

    public record DiagnosticoDbRecord(
        Guid Id,
        Guid OrdemProducaoId,
        string NomeMecanico,
        string? LaudoTecnico);

    public record TarefaProducaoDbRecord(
        Guid Id,
        Guid OrdemProducaoId,
        string Descricao,
        string Status,
        int TempoEstimadoMinutos);

    public class ExecutionDbHelper
    {
        private readonly string _connectionString;

        public ExecutionDbHelper() : this(TestConfiguration.Current) { }

        public ExecutionDbHelper(TestConfiguration config)
        {
            _connectionString = config.ExecutionDbConnectionString;
        }

        public async Task<OrdemProducaoDbRecord?> ObterOrdemProducaoAsync(Guid osId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, ordem_servico_id, status, versao, cliente_nome, veiculo_placa
                FROM ordens_producao
                WHERE ordem_servico_id = @osId";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("osId", osId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new OrdemProducaoDbRecord(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    reader.GetString(5));
            }

            return null;
        }

        public async Task<DiagnosticoDbRecord?> ObterDiagnosticoAsync(Guid ordemProducaoId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, ordem_producao_id, nome_mecanico, laudo_tecnico
                FROM diagnosticos
                WHERE ordem_producao_id = @opId";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("opId", ordemProducaoId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new DiagnosticoDbRecord(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3));
            }

            return null;
        }

        public async Task<IReadOnlyList<TarefaProducaoDbRecord>> ObterTarefasAsync(Guid ordemProducaoId)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, ordem_producao_id, descricao, status, tempo_estimado_minutos
                FROM tarefas_producao
                WHERE ordem_producao_id = @opId";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("opId", ordemProducaoId);

            var list = new List<TarefaProducaoDbRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TarefaProducaoDbRecord(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4)));
            }

            return list;
        }
    }
}
