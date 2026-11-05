namespace Fiap.TechChallenge.E2ETests.Support
{
    public class E2ETestContext
    {
        public string? AdminToken { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public string ClienteTelefone { get; set; } = "11987654321";
        public Guid VeiculoId { get; set; }
        public string VeiculoPlaca { get; set; } = string.Empty;
        public string VeiculoModelo { get; set; } = string.Empty;
        public int VeiculoAno { get; set; } = 2020;
        public Guid OrdemServicoId { get; set; }
        public Guid OrdemProducaoId { get; set; }
        public Guid TarefaProducaoId { get; set; }
        public Guid ServicoId { get; set; }
        public string ServicoNome { get; set; } = string.Empty;
        public Guid PecaId { get; set; }
        public string PecaNome { get; set; } = string.Empty;

        // Billing Lifecycle State
        public Guid? OrcamentoId { get; set; }
        public Guid? PagamentoId { get; set; }
        public string? ExternalPaymentId { get; set; }
        public decimal? ValorOrcamento { get; set; }

        public HttpResponseMessage? UltimaResposta { get; set; }
        public string? UltimoCorpoResposta { get; set; }
    }
}

