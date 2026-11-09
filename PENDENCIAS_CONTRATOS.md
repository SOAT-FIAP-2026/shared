# Pendências para centralizar os contratos no `shared`

> Levantamento de 2026-11-09 sobre o `shared` 2.0.0 (PR [#2](https://github.com/SOAT-FIAP-2026/shared/pull/2), branch `feat/espelho-contratos`) e a `main` de cada serviço. Caminhos relativos à raiz de cada repositório.

O `shared` já espelha o formato que os serviços colocam no fio, e as fixtures em `tests/Fiap.TechChallenge.Contracts.Tests/Fixtures/wire/` provam isso tipo a tipo. Falta o outro lado: os serviços ainda declaram cópias locais desses contratos, alguns contratos divergem entre quem produz e quem consome, e o próprio `shared` tem lacunas. Este documento lista o que falta para o `shared` virar a fonte única.

Ordem sugerida: [1](#1-bloqueio-quebras-ao-mergear-o-pr-2) → [4](#4-divergências-entre-serviços-decidir-antes-de-centralizar) → [2](#2-cópias-locais-que-já-têm-espelho-no-shared) → [3](#3-lacunas-no-próprio-shared) e [5](#5-contratos-do-shared-sem-tráfego-implementar-ou-apagar).

---

## 1. Bloqueio: quebras ao mergear o PR #2

Os serviços compilam contra `shared/main` sem versão fixa (`ProjectReference` para `../../../shared`), então o CI deles fica vermelho no merge até estes ajustes:

| Serviço | Arquivo | Erro | Ajuste |
|---|---|---|---|
| os-microservice | `src/OrdemServico.Lambda/Infrastructure/Http/CoreApiClient.cs:24` | CS1061: `ClienteCatalogoDto` sem `Telefone` | usar `Celular` |
| fase1-tech-challenge | `src/Fiap.TechChallenge.Application/DTOs/Responses/ClienteResponse.cs:31` | CS7036: falta `Celular` | `new(Id, Nome, CpfCnpj, Email, Celular)`; no construtor da linha 27, trocar `string.Empty` por `dto.CpfCnpj` |
| fase1-tech-challenge | `src/Fiap.TechChallenge.Application/DTOs/Responses/ServicoResponse.cs:27` | CS1503: ordem dos argumentos | `new(Id, Nome, Descricao, ValorUnitario, TempoEstimadoMinutos)` |
| billing-service | `tests/Billing.Service.Tests/Contracts/BillingServiceContractTests.cs:129-131` | compila; `Enums_de_billing_devem_manter_os_valores_do_contrato` falha | esperar `Pendente, Aprovado, Rejeitado, Cancelado, Expirado` |
| execution-service | — | compila | — |
| lamdba-auth | — | não referencia o `shared` | — |

No fase1 e no OS, estes são só os primeiros erros: os projetos de teste só compilam depois do `src`, e podem aparecer falhas de teste em tempo de execução.

---

## 2. Cópias locais que já têm espelho no `shared`

Cada linha tem fixture capturada do tipo local, então trocar pelo tipo do `shared` não muda o JSON.

### os-microservice

| Tipo local | Arquivo | Usar do `shared` | Observação |
|---|---|---|---|
| `EventoDominio` e os 6 eventos do outbox (`OrdemServicoCriada`, `StatusOrdemServicoAlterado`, `PecasInsumoIncluidas`, `OrcamentoReprovado`, `OrdemServicoCancelada`, `CompensacaoExecutada`) | `src/OrdemServico.Lambda/Domain/Events/EventosDominio.cs` | `OsService.DomainEvents.*` | mesmos nomes; importar o sub-namespace e apagar o arquivo local |
| `StatusOS` (Id, código, descrição) | `src/OrdemServico.Lambda/Domain/Entities/StatusOS.cs` | `OrdemServicoStatus` (constantes) e `OrdemServicoStatus.Catalogo` | Ids fixos idênticos; a entidade pode ficar, mas os códigos devem vir das constantes |
| `JsonDefaults` | `src/OrdemServico.Lambda/Configuration/JsonDefaults.cs` | `WireJsonOptions.OsEventos` | as opções do `shared` são somente leitura; quem precisar alterar cria cópia com `new JsonSerializerOptions(WireJsonOptions.OsEventos)` |
| `ErrorResponse` | `src/OrdemServico.Lambda/Api/Middlewares.cs` | `Common.ErrorResponse` | — |
| objeto anônimo `{ id, clienteNotificado }` | `src/OrdemServico.Lambda/Api/OrdemServicoEndpoints.cs:24` | `OrdemServicoCriadaResponse` | — |

### fase1-tech-challenge

O fio usa os DTOs locais de `src/Fiap.TechChallenge.Application/DTOs/` e converte para o `shared` com operadores implícitos (`ToContract()`). Centralizar significa os controllers receberem e devolverem o tipo do `shared` e os operadores sumirem.

| Tipo local | Usar do `shared` |
|---|---|
| `Responses/ClienteResponse` | `ClienteCatalogoDto` / `ClienteDetalhadoDto` (mesma forma) |
| `Responses/VeiculoResponse` | `VeiculoCatalogoDto` |
| `Responses/ServicoResponse` | `ServicoCatalogoDto` |
| `Responses/PecaInsumoResponse` | `PecaInsumoCatalogoDto` |
| `Responses/ServicoMetricaTempoResponse` | `ServicoMetricaTempoDto` |
| `Responses/EstoqueResponse` | `EstoqueDetalhadoDto` (nenhum endpoint devolve hoje) |
| `Common/PagedResult<T>` | `PagedResultContrato<T>` |
| `Requests/ClienteRequest` | `CriarClienteRequest` / `AtualizarClienteRequest` |
| `Requests/VeiculoRequest`, `VeiculoPatchRequest` | `CriarVeiculoRequest`, `AtualizarVeiculoPatchRequest` |
| `Requests/ServicoRequest`, `PecaInsumoRequest`, `EstoqueRequest` | `CriarServicoRequest`, `CriarPecaInsumoRequest`, `CriarEstoqueRequest` |
| `Requests/LoginRequest`, `LoginResponse`, `CadastrarRequest` | `StaffLoginRequest`, `StaffLoginResponse`, `StaffCadastrarRequest` |
| `ErrorResponse` aninhado em `src/Fiap.TechChallenge.Api/Middlewares/ExceptionHandlingMiddleware.cs` | `Common.ErrorResponse` |

Respostas com id de criação (`{ "id": … }`) têm `IdCriadoResponse` no `shared`; nenhum serviço usa ainda.

### billing-service

| Tipo local | Arquivo | Usar do `shared` | Observação |
|---|---|---|---|
| entidade `Orcamento` serializada direto | `src/Billing.Service.Domain/Entities/Orcamento.cs`; `Ok(orcamento)` em `src/Billing.Service.Api/Controllers/V1/OrcamentoController.cs:27,37,47` | `OrcamentoResponse` + `ValorMonetarioResponse` | a entidade de domínio vaza no fio; mapear no controller |
| `PagamentoStatus` | `src/Billing.Service.Domain/Entities/Pagamento.cs:5` | `StatusPagamentoContrato` | mesmos nomes e valores |
| `ErrorResponse` aninhado | `src/Billing.Service.Api/Middlewares/ExceptionHandlingMiddleware.cs` | `Common.ErrorResponse` | — |
| `GerarOrcamentoRequest` (classe local) | `src/Billing.Service.Application/DTOs/Requests/GerarOrcamentoRequest.cs` | já usa o do `shared` no controller | cópia morta, com forma diferente (`Servico`/`PecaInsumo` de domínio); só `tests/.../OrcamentoServiceTests.cs:234` usa. Apagar |
| webhook lido como `JsonElement` | `src/Billing.Service.Api/Controllers/V1/PagamentoController.cs:39-46` | `MercadoPagoWebhookRequest` + `MercadoPagoWebhookParametros` | o `shared` aceita `id`/`status` como texto ou número; nomes de header e manifesto da assinatura já estão lá |

### execution-service

| Tipo local | Arquivo | Usar do `shared` | Observação |
|---|---|---|---|
| 11 respostas (`OrdemProducaoResponse`, `TarefaProducaoResponse`, `DiagnosticoResponse`, `OrdemProducaoFilaItemResponse`, `HistoricoProducaoResponse`, `RelatorioProdutividadeResponse`, `MetricaProdutividadeMecanicoResponse`, `PecaSugeridaResponse`, `ServicoSugeridoResponse`, `ClienteSnapshotResponse`, `VeiculoSnapshotResponse`) | `src/Fiap.TechChallenge.Execution.Application/DTOs/ProducaoResponses.cs` | `ExecutionService.Responses.*` | mesmos nomes; importar o sub-namespace e apagar o arquivo |
| enums `StatusOrdemProducao`, `StatusTarefaProducao` (vão como texto UPPER_SNAKE) | `src/Fiap.TechChallenge.Execution.Domain/Aggregates/OrdemProducaoAggregate/` | `StatusOrdemProducaoContrato`, `StatusTarefaProducaoContrato` | o enum fica no domínio; o `shared` dá as strings a quem consome |
| ProblemDetails montado no middleware | `src/Fiap.TechChallenge.Execution.Api/Middlewares/ExceptionHandlingMiddleware.cs` | `ProblemDetailsResponse` | — |

### lamdba-auth

Não referencia o `shared`. Falta adicionar o `ProjectReference` e o checkout do `shared` no CI, como nos outros serviços.

| Local | Arquivo | Usar do `shared` |
|---|---|---|
| `AuthRequest`, `AuthResponse` | `src/Fiap.TechChallenge.LambdaAuth/Models/` | `AuthTokenRequest`, `AuthTokenResponse` (snake_case) |
| objeto anônimo `{ error }` | `src/Fiap.TechChallenge.LambdaAuth/Function.cs:92,141` | `AuthErrorResponse` |
| defaults e variáveis de ambiente `JWT_*` | `src/Fiap.TechChallenge.LambdaAuth/Function.cs:36-40` | `JwtAuthConstants.LambdaAuth` |

---

## 3. Lacunas no próprio `shared`

- **Nomes de evento como constantes.** Os consumidores fazem `switch` em texto literal (`execution-service/.../ExecutionSqsBackgroundWorker.cs:114,127`, `os-microservice/.../OsSqsBackgroundWorker.cs:173,177`, `billing-service/.../BillingSqsBackgroundWorker.cs:133`, `os-microservice/.../CompensacaoEventoFunction.cs:88-114`), e o filtro SNS repete os nomes em `os-microservice/infra/template.yaml:493`. Um catálogo de `tipoEvento` no `shared` teria evitado os nomes que ninguém emite (seção 4).
- **Recaptura das fixtures do OS e do Core.** Hoje só foram conferidas na captura inicial; recapturar depois que esses serviços compilarem contra a 2.0.0 (passo a passo no README, "Como regenerar as fixtures").
- **Pontos cegos da comparação (`WireShape`).** Não detecta diferença em membros de enum, valores de dicionário, `byte[]`, conversor declarado no tipo, `required`, genéricos fechados e nulabilidade de elemento de lista (`List<string?>` = `List<string>`). Nenhum tipo atual é afetado.
- **`WireFixture` ignora o campo `opcoes`** gravado na fixture; o teste escolhe as opções por conta própria.
- **Teste de status do execution** deriva os códigos por regex em vez do `ToUpperSnake()` real do serviço.
- **`MercadoPagoWebhookRequest`** escreve `id`/`status` nulos na raiz quando usado para montar um payload.
- **Documentação.** O README diz que `WireJsonOptions.Http` vale para todos os serviços, mas o execution acrescenta enum como texto. Sobrou `autenticacao/login` em minúsculo em `AuthContracts.cs:79,82,85` e no README.
- **`e2e.yml`** vem com URLs `localhost` por padrão, que não funcionam num runner do GitHub.

---

## 4. Divergências entre serviços (decidir antes de centralizar)

O `shared` documenta o que trafega; nestes casos produtor e consumidor não concordam, e alguém precisa mudar.

| Divergência | Onde | Decisão pendente |
|---|---|---|
| **Issuer/audience JWT** | `lamdba-auth/src/.../Function.cs:38-39` e `soat-infra/environments/dev/main.tf` usam `fiap-tech-challenge`/`fiap-api`; os e execution validam `TechChallenge`/`techchallenge.com.br` (`JwtAuthConstants.DefaultIssuer`/`DefaultAudience`). O token do lambda-auth também não tem role | escolher um par e deixar uma constante só no `shared`; incluir role no token |
| **Dois envelopes no mesmo tópico** | `EventoDominio` (`idOrdemServico`, `idEvento`, `ocorridoEm`, camelCase) e `IntegrationEvent` (`osId`, `eventId`, `occurredAt`; camelCase no OS, PascalCase no billing/execution). O execution aceita os dois nomes (`case "OrdemServicoCriada"`) | adotar um envelope e um casing para o tópico |
| **Compensação espera nomes que ninguém emite** | filtro em `os-microservice/infra/template.yaml:493` e `CompensacaoEventoFunction.cs:90-91` esperam `ReservaEstoqueFalhou`/`PagamentoRecusado`; o billing emite `PagamentoCanceladoCompensacaoEvent` e o Core não publica no SNS | alinhar filtro e função ao que o billing emite; decidir se o Core passa a publicar falha de reserva |
| **Eventos consumidos sem produtor** | `DiagnosticoRejeitadoEvent` (consumido em `execution-service/.../DiagnosticoRejeitadoConsumer.cs`) e `DiagnosticoFinalizadoEvent` (consumido em `billing-service/.../BillingSqsBackgroundWorker.cs:117`); o finalizar-diagnóstico do OS emite só `StatusOrdemServicoAlterado` | o OS passa a publicar, ou os consumidores mudam de evento |
| **Link de pagamento quebrado** | `OrcamentoGeradoEvent.LinkPagamento` aponta para `api/v1/pagamento/checkout/{id}`, rota que não existe no billing | criar a rota ou trocar o link |
| **Ajuste de estoque** | os endpoints do Core recebem um número JSON cru (`[FromBody] int`); `AjustarQuantidadeEstoqueRequest` (`{"quantidade":n}`) seria rejeitado com 400 | mudar o endpoint para o objeto ou apagar o contrato |

---

## 5. Contratos do `shared` sem tráfego (implementar ou apagar)

Ficaram no pacote com `<remarks>Não trafega hoje: …</remarks>`. Cada um precisa de decisão: implementar no serviço ou remover do `shared`.

| Contrato | Arquivo no `shared` | Situação |
|---|---|---|
| `DiagnosticoFinalizadoEvent`, `DiagnosticoRejeitadoEvent` | `OsService/OrdemServicoEvents.cs` | têm consumidor, não têm produtor (seção 4) |
| `OrcamentoReprovadoEvent`, `PecasInsumoIncluidasEvent`, `StatusOrdemServicoAlteradoEvent`, `CompensacaoExecutadaEvent` | `OsService/OrdemServicoEvents.cs` | gêmeos dos eventos de `DomainEvents`, que são os que o OS publica |
| `ClienteContrato`, `VeiculoContrato`, `TarefaItemContrato` | `OsService/OrdemServicoModels.cs` | ninguém usa; no fio vão `ClienteInfo`, `VeiculoInfo`, `TarefaInfo` |
| `ItemOrcamentoInfo` | `OsService/OrdemServicoModels.cs` | só aparece em `DiagnosticoFinalizadoEvent` |
| `OrdemServicoStatus.APROVADA`, `REJEITADA` | `OsService/OrdemServicoStatus.cs` | o OS emite `ORCAMENTO_APROVADO`/`ORCAMENTO_REPROVADO` |
| `PagamentoRecusadoEvent` | `BillingService/BillingEvents.cs` | o billing publica `PagamentoCanceladoCompensacaoEvent` para rejeitado, cancelado e expirado |
| `MetodoPagamentoContrato` | `BillingService/BillingModels.cs` | o billing guarda o meio como texto (sempre `"boleto"`) |
| `ReservaEstoqueFalhouEvent` | `CoreService/CoreEstoqueEvents.cs` | o Core não publica no SNS |
| `AjustarQuantidadeEstoqueRequest`, `EstoqueDetalhadoDto` | `CoreService/CoreCatalogoContracts.cs` | endpoint recebe `int` cru; nenhum endpoint devolve estoque detalhado |
