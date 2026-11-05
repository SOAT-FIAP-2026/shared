# Plano de Implementação Futura — Testes BDD E2E com Billing Service e Coreografia Assíncrona Completa

**Repositório:** `shared` (`Fiap.TechChallenge.E2ETests` e `Fiap.TechChallenge.Contracts`)  
**Status:** Planejado para Próxima Iteração  
**Objetivo:** Expandir a suíte de testes E2E BDD para cobrir integralmente o `billing-service` (Orçamentos, Pagamentos/Boletos, Webhooks e Eventos SNS/SQS) e eliminar sincronizações HTTP manuais nos steps dos testes, garantindo que 100% da coreografia entre microsserviços seja validada sem atalhos.

---

## 1. Diagnóstico do Estado Atual

Atualmente, a suíte `Fiap.TechChallenge.E2ETests` possui 3 features que rodam contra os contêineres reais (`Core`, `OS`, `Execution`, `Floci` e bancos `PostgreSQL`/`DynamoDB`), porém:

1. **Ausência de Cobertura E2E do `billing-service`:**
   - Embora o contêiner `techchallenge-billing-service` (`localhost:8081`) e o banco `techchallenge-billing-db` (`localhost:5434`) subam no `docker-compose.yml` (profile `full`) e as filas `techchallenge-billing-eventos-local` já sejam criadas no Floci (`01-init-aws.sh`), nenhum cenário `.feature` exercita os endpoints ou eventos do `billing-service`.
   - `TestConfiguration.cs` e `appsettings.json` ainda não expõem `BillingApiUrl` (`http://localhost:8081`) nem `BillingDbConnectionString` (`Port=5434`).

2. **Atalhos Síncronos nos Steps de Retorno (`Execution -> OS`):**
   - Em `CicloCompletoOrdemServicoSteps.cs`, enquanto o sentido `OS -> Execution` é validado via eventos reais SNS/SQS (`OrdemServicoAbertaEvent` e `OrdemServicoLiberadaParaExecucaoEvent`), o sentido inverso (`Execution -> OS`) faz chamadas `PATCH` manuais diretamente nos endpoints do `os-microservice` para espelhar `EM_DIAGNOSTICO`, `AGUARDANDO_APROVACAO` e `FINALIZADA`.

3. **Compensação SAGA Limitada à Validação Síncrona:**
   - Em `2_SagaCompensacaoFalhaEstoque.feature`, o step valida apenas a rejeição síncrona HTTP `4xx` ao tentar inserir uma peça inexistente, sem exercitar o fluxo assíncrono de compensação via fila SQS (`PagamentoRecusadoEvent`, `PagamentoCanceladoCompensacaoEvent`, `ProducaoCanceladaEvent` -> `PagamentoEstornadoEvent`).

---

## 2. Pré-requisitos nos Microsserviços (Antes ou Junto ao BDD)

Para que os cenários E2E BDD rodem 100% com código real e sem mocks, os seguintes pontos técnicos devem estar ativos no `billing-service` e no `os-microservice`:

1. **Workers SQS e Publishers SNS no `billing-service`:**
   - Consumir da fila `techchallenge-billing-eventos-local` os eventos `DiagnosticoTecnicoConcluidoEvent` (ou requisição de orçamento), `ProducaoCanceladaEvent` e `OrdemServicoCanceladaEvent`.
   - Publicar no tópico SNS `techchallenge-eventos-local` (usando `SnsMessageAttributes` do pacote `Fiap.TechChallenge.Contracts.Common`):
     - `OrcamentoGeradoEvent`
     - `PagamentoConfirmadoEvent`
     - `PagamentoRecusadoEvent` / `PagamentoCanceladoCompensacaoEvent`
     - `PagamentoEstornadoEvent`
2. **Desacoplamento Final no `OrcamentoController.cs` (`billing-service`):**
   - Atualizar a action `POST /api/v1/orcamento` para receber diretamente `Fiap.TechChallenge.Contracts.BillingService.GerarOrcamentoRequest` (atualmente ainda faz alias para `Billing.Service.Application.DTOs.Requests.GerarOrcamentoRequest`).
   - Corrigir o parâmetro de rota `[HttpPost("{id:guid}/enviar-orcamento")]` para casar com `Guid id` (atualmente o parâmetro na assinatura está como `Guid idOrcamento`, divergindo do template `{id:guid}`).
3. **Worker SQS de Eventos de Domínio no `os-microservice`:**
   - Garantir que o consumidor local no container `os-service` processe continuamente a fila `techchallenge-os-eventos-local` para reagir automaticamente a `DiagnosticoIniciadoEvent`, `DiagnosticoTecnicoConcluidoEvent`, `OrcamentoGeradoEvent`, `PagamentoConfirmadoEvent` e `ProducaoFinalizadaEvent` sem precisar de `PATCH` manual do teste BDD.

---

## 3. Alterações Necessárias na Infraestrutura de Testes (`shared/tests/Fiap.TechChallenge.E2ETests`)

### 3.1. Configuração (`appsettings.json` e `TestConfiguration.cs`)
Adicionar suporte às URLs e banco de dados do `billing-service`:

```json
{
  "Urls": {
    "CoreApi": "http://localhost:8080",
    "OsApi": "http://localhost:8083",
    "ExecutionApi": "http://localhost:8082",
    "BillingApi": "http://localhost:8081",
    "Floci": "http://localhost:4566"
  },
  "ConnectionStrings": {
    "ExecutionDb": "Host=localhost;Port=5433;Database=techchallenge_execution_db;Username=execution;Password=execution_local_pwd",
    "BillingDb": "Host=localhost;Port=5434;Database=techchallenge_billing_db;Username=billing;Password=billing_password"
  }
}
```

### 3.2. Novo Helper de Banco de Dados (`Support/BillingDbHelper.cs`)
Criar a classe `BillingDbHelper` (nos moldes do `ExecutionDbHelper.cs`) utilizando `Npgsql` para inspecionar fisicamente as tabelas do PostgreSQL do `billing-service`:
- `ObterOrcamentoPorOrdemServicoAsync(Guid osId)` -> valida criação, status e `valor_total` na tabela de orçamentos.
- `ObterPagamentoPorOrcamentoAsync(Guid orcamentoId)` -> valida persistência do boleto/PIX, `status`, `payment_id`, `boleto_url` e `codigo_barras` na tabela de pagamentos.

### 3.3. Contexto Compartilhado (`Support/E2ETestContext.cs`)
Adicionar propriedades de rastreio do ciclo financeiro:
- `Guid OrcamentoId { get; set; }`
- `Guid PagamentoId { get; set; }`
- `string ExternalPaymentId { get; set; }`
- `decimal ValorOrcamento { get; set; }`

---

## 4. Novos Cenários BDD e Evoluções Planejadas

### 4.1. Atualização de `3_SanidadeEContratosMicrosservicos.feature`
Incluir o `billing-service` nas validações de saúde e infraestrutura:
- Consultar o endpoint `/health/live` (ou `/health`) do `Billing Service` (`http://localhost:8081`) e validar `HTTP 200 OK`.
- Validar no `SanidadeSteps.cs` que a fila `techchallenge-billing-eventos-local` e sua respectiva DLQ existem no Floci.

### 4.2. Nova Feature: `4_FluxoFaturamentoEPagamentoOrdemServico.feature`
Criar feature dedicada ao ciclo de faturamento e integração com contratos compartilhados (`Fiap.TechChallenge.Contracts.BillingService` e `Fiap.TechChallenge.Contracts.Events`):

```gherkin
# language: pt
Funcionalidade: Faturamento, Orçamento e Pagamento da Ordem de Serviço (Billing Service)
  Como um cliente da oficina mecânica
  Eu quero receber o orçamento calculado pelo Billing Service e realizar o pagamento
  Para que minha Ordem de Serviço tenha o orçamento aprovado e seja liberada para execução

  Contexto:
    Dado que os microsserviços Core, OS, Execução e Billing estão operacionais
    E que o usuário autentica como Administrador no Core Service
    E que existe um cliente e um veículo cadastrados no catálogo

  Cenário: Geração de orçamento, emissão de boleto, confirmação via Webhook e liberação automática da OS
    Dado que existe uma Ordem de Serviço em fase de diagnóstico concluído com serviços e peças
    Quando o orçamento é solicitado no Billing Service utilizando o contrato compartilhado GerarOrcamentoRequest
    Então o orçamento é gravado no PostgreSQL do Billing Service com o valor total calculado corretamente
    E o evento OrcamentoGeradoEvent é publicado no SNS com os atributos canônicos
    Quando o cliente solicita a emissão de boleto no Billing Service via CriarBoletoRequest
    Então o pagamento é registrado com status "Pendente" no PostgreSQL do Billing Service
    Quando o Billing Service recebe a notificação de pagamento aprovado no endpoint de Webhook
    Então o status do pagamento no PostgreSQL do Billing Service é atualizado para "Aprovado"
    E o evento PagamentoConfirmadoEvent é consumido pelo OS Service atualizando a OS no DynamoDB para "ORCAMENTO_APROVADO"
    E o Execution Service recebe automaticamente a liberação via SQS gravando as tarefas com status "NA_FILA"
```

### 4.3. Expansão de `2_SagaCompensacaoFalhaEstoque.feature`
Adicionar um segundo cenário na feature de SAGA para testar a **Compensação Assíncrona Real via Eventos**:

```gherkin
  Cenário: Compensação assíncrona na OS quando o pagamento é recusado ou cancelado no Billing Service
    Dado que o atendente possui uma Ordem de Serviço aguardando pagamento com peças reservadas no estoque
    Quando o pagamento é recusado no Billing Service disparando PagamentoCanceladoCompensacaoEvent no SNS
    Então a Lambda de compensação do OS Service consome o evento da fila SQS
    E o estoque da peça reservada é estornado no Core Service
    E o status da Ordem de Serviço no DynamoDB é atualizado para "CANCELADA"
```

### 4.4. Remoção dos `PATCH` Manuais em `1_CicloCompletoOrdemServico.feature`
No `CicloCompletoOrdemServicoSteps.cs`:
- Remover as chamadas manuais `PATCH /iniciar-diagnostico`, `PATCH /finalizar-diagnostico` e `PATCH /servicos/{id}/finalizar` feitas diretamente pelo teste no `os-microservice`.
- Substituir por polling no DynamoDB aguardando que os eventos `DiagnosticoIniciadoEvent`, `DiagnosticoTecnicoConcluidoEvent` e `ProducaoFinalizadaEvent` publicados pelo `execution-service` atualizem autonomamente o estado da OS no `os-microservice`.

---

## 5. Checklist de Execução para o Desenvolvedor / Agente

- [ ] **Passo 1:** Corrigir no `billing-service` o binding de `GerarOrcamentoRequest` (usar contrato do `shared`) e o parâmetro de rota `{id:guid}` no `OrcamentoController.cs`.
- [ ] **Passo 2:** Implementar/ativar o publisher SNS e consumer SQS no `billing-service` usando `Fiap.TechChallenge.Contracts.Events`.
- [ ] **Passo 3:** Adicionar `BillingApiUrl` e `BillingDbConnectionString` em `TestConfiguration.cs` e `appsettings.json` do projeto `Fiap.TechChallenge.E2ETests`.
- [ ] **Passo 4:** Criar `BillingDbHelper.cs` em `shared/tests/Fiap.TechChallenge.E2ETests/Support/`.
- [ ] **Passo 5:** Atualizar `SanidadeSteps.cs` para validar health e filas do `billing-service`.
- [ ] **Passo 6:** Criar `4_FluxoFaturamentoEPagamentoOrdemServico.feature` e `FaturamentoPagamentoSteps.cs`.
- [ ] **Passo 7:** Adicionar cenário de compensação assíncrona via SQS em `2_SagaCompensacaoFalhaEstoque.feature`.
- [ ] **Passo 8:** Executar `docker compose --profile full up -d --build` e validar `dotnet test` com 100% de aprovação sem chamadas de espelhamento manual.
