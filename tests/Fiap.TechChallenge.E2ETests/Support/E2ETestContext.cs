namespace Fiap.TechChallenge.E2ETests.Support
{
    public class E2ETestContext
    {
        public string? AdminToken { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public Guid VeiculoId { get; set; }
        public string VeiculoPlaca { get; set; } = string.Empty;
        public Guid OrdemServicoId { get; set; }
        public Guid OrdemProducaoId { get; set; }
        public Guid ServicoId { get; set; }
        public Guid PecaId { get; set; }
        public HttpResponseMessage? UltimaResposta { get; set; }
        public string? UltimoCorpoResposta { get; set; }
    }
}

