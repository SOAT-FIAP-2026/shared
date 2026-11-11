# Espelho de contratos no `shared` — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fazer `Fiap.TechChallenge.Contracts` espelhar exatamente os contratos que os serviços trocam hoje.

**Architecture:** Fixtures de "forma no fio" são capturadas dos tipos reais dos serviços por uma ferramenta descartável (scratchpad) usando a mesma visão do `System.Text.Json` (`JsonTypeInfo`). Os testes do `shared` comparam a forma de cada tipo do `shared` com a fixture correspondente. Cada área (Common, OS, Core, Billing, Execution, Auth) é ajustada até bater.

**Tech Stack:** .NET 8, C# 12, System.Text.Json, xUnit 2.4, FluentAssertions 6.12, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-08-espelho-contratos-design.md`

## Global Constraints

- `net8.0`; só `System.Text.Json` (sem Newtonsoft).
- Só arquivos dentro de `D:\Projetos\FIAP\shared` mudam. A ferramenta de captura vive no scratchpad e nunca é commitada.
- Tipos novos que têm homônimo local em serviço vão para sub-namespace que nenhum serviço importa: `Fiap.TechChallenge.Contracts.OsService.DomainEvents`, `Fiap.TechChallenge.Contracts.ExecutionService.Responses`.
- Nenhum contrato é apagado. Órfão recebe `<remarks>Não trafega hoje: {motivo}.</remarks>`. Sem `[Obsolete]`.
- Divergência que só se resolve mudando serviço: registrar em `<remarks>`, não escolher lado.
- `Version` do pacote = `2.0.0`. Sem tag, sem publicação.
- Commits locais na branch `feat/espelho-contratos`. Push e PR só com ok do usuário.
- Testes: xUnit + FluentAssertions, nomes de método em português com `_`. Sem dado pessoal real (CPF fake `"00000000000"`).
- Fontes da verdade: caminhos relativos a `D:\Projetos\FIAP\`, citados em cada task.

## Review Focus

1. **Colisão de nome em serviço:** tipo novo com mesmo nome simples de um tipo local, num namespace que o serviço já importa, gera CS0104 no serviço. Esperado: homônimos só nos sub-namespaces acima. Teste em Task 4 e Task 8 (`Tipos_homonimos_ficam_em_sub_namespace`).
2. **`[JsonPropertyName]` em record posicional sem `property:`:** atributo vai para o parâmetro e é ignorado na serialização. Esperado: nome snake_case no fio. Pego pela fixture em Task 9 (`AuthTokenResponse_espelha_lambda_auth`).
3. **Valor numérico de enum:** reordenar membros muda o int no fio. Esperado: valores explícitos iguais aos do serviço. Teste em Task 7 (`StatusPagamentoContrato_espelha_valores_do_billing`).
4. **`WireJsonOptions` mutável:** consumidor altera instância compartilhada e quebra outro. Esperado: instâncias read-only. Teste em Task 2 (`WireJsonOptions_sao_read_only`).
5. **Nulabilidade perdida:** `string?` vs `string` não muda o JSON mas muda o contrato. Esperado: `WireShape` marca `?`. Teste em Task 2 (`WireShape_marca_nulabilidade`).

---

### Task 1: Ambiente, CI e versão

**Files:**
- Modify: `.github/workflows/ci-publish.yml` (step `Test`)
- Create: `.github/workflows/e2e.yml`
- Modify: `src/Fiap.TechChallenge.Contracts/Fiap.TechChallenge.Contracts.csproj` (`<Version>`)
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/PacoteTests.cs`

**Interfaces:**
- Produces: SDK .NET 8 instalado; `dotnet test tests/Fiap.TechChallenge.Contracts.Tests` como comando padrão de teste.

- [ ] **Step 1: Instalar SDK (pedir ok ao usuário antes)**

Run: `winget install Microsoft.DotNet.SDK.8 --silent --accept-package-agreements --accept-source-agreements` e depois `dotnet --list-sdks`
Expected: linha começando com `8.0.`

- [ ] **Step 2: Baseline**

Run: `dotnet test tests/Fiap.TechChallenge.Contracts.Tests -c Release`
Expected: `Passed!` com 0 falhas. Anotar a contagem.

- [ ] **Step 3: Teste de versão falhando**

```csharp
public class PacoteTests
{
    [Fact]
    public void Versao_do_pacote_deve_ser_2_0_0() =>
        typeof(IntegrationEvent).Assembly.GetName().Version!.ToString(3).Should().Be("2.0.0");
}
```

Run: `dotnet test tests/Fiap.TechChallenge.Contracts.Tests --filter Versao_do_pacote_deve_ser_2_0_0` — Expected: FAIL (`1.1.0`).

- [ ] **Step 4: `<Version>2.0.0</Version>` no csproj; rodar o teste** — Expected: PASS.

- [ ] **Step 5: CI**
  - `ci-publish.yml`, step `Test`: `dotnet test tests/Fiap.TechChallenge.Contracts.Tests/Fiap.TechChallenge.Contracts.Tests.csproj -c Release --no-build --verbosity normal`.
  - `e2e.yml`: `name: E2E (manual)`, `on: workflow_dispatch`, job `ubuntu-latest` com checkout, `setup-dotnet` 8.0.x e `dotnet test tests/Fiap.TechChallenge.E2ETests -c Release`. Se `tests/Fiap.TechChallenge.E2ETests/Support/TestConfiguration.cs` aceitar override por variável de ambiente, expor cada URL como `inputs` do dispatch e repassar em `env`.

- [ ] **Step 6: Commit** — `ci: run only contract tests on CI and move E2E to manual workflow` (inclui bump de versão).

---

### Task 2: `WireJsonOptions`, `WireShape` e captura de fixtures

**Files:**
- Create: `src/Fiap.TechChallenge.Contracts/Common/WireJsonOptions.cs`
- Create: `tests/Fiap.TechChallenge.Contracts.Tests/Support/WireShape.cs`
- Create: `tests/Fiap.TechChallenge.Contracts.Tests/Support/WireFixture.cs`
- Create: `tests/Fiap.TechChallenge.Contracts.Tests/Fixtures/wire/*.json` (gerados)
- Modify: `tests/Fiap.TechChallenge.Contracts.Tests/Fiap.TechChallenge.Contracts.Tests.csproj` (`<None Include="Fixtures\**\*.json" CopyToOutputDirectory="PreserveNewest" />`)
- Create (fora do repo): `<scratchpad>\wire-capture\WireCapture.csproj`, `Program.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Support/WireShapeTests.cs`

**Interfaces:**
- Produces:
  - `public static class WireJsonOptions { JsonSerializerOptions OsEventos { get; } JsonSerializerOptions PadraoDotNet { get; } JsonSerializerOptions Http { get; } }` (namespace `Fiap.TechChallenge.Contracts.Common`).
    - `OsEventos`: `JsonSerializerDefaults.Web` + `DefaultIgnoreCondition = WhenWritingNull` + `JsonStringEnumConverter()` (cópia de `os-microservice/src/OrdemServico.Lambda/Configuration/JsonDefaults.cs:9-13`).
    - `PadraoDotNet`: `new JsonSerializerOptions()` (eventos do billing e execution, resposta do lambda-auth).
    - `Http`: `JsonSerializerDefaults.Web` (controllers/minimal APIs).
    - Todas com `TypeInfoResolver = new DefaultJsonTypeInfoResolver()` e `MakeReadOnly()`.
  - `public static class WireShape { static JsonObject Extrair(Type tipo, JsonSerializerOptions opcoes); static string ParaJson(JsonNode forma); }` (namespace `Fiap.TechChallenge.Contracts.Tests.Support`).
  - `public static class WireFixture { static JsonNode Forma(string nome); static void DeveEspelhar<T>(string nome, JsonSerializerOptions opcoes); }`.
  - Fixtures nomeadas `{area}.{TipoFonte}.json` com `{ "fonte": "repo/caminho:linha", "tipo": "<FullName>", "opcoes": "OsEventos|PadraoDotNet|Http", "forma": { ... } }`.

**Regras do `WireShape.Extrair`** (decisão do plano):
- Propriedades = `opcoes.GetTypeInfo(tipo).Properties` (nome já com naming policy e `[JsonPropertyName]`; ignoradas já fora).
- Tipo de cada propriedade → forma:
  - `string`, `Guid`, `DateTime`, `DateTimeOffset`, `char` → `"string"`
  - numéricos → `"number"`; `bool` → `"boolean"`
  - enum → `"enum:string"` se `opcoes.Converters` tem `JsonStringEnumConverter`, senão `"enum:number"`
  - `IEnumerable<T>` (exceto string) → `JsonArray` com 1 elemento = forma de `T`
  - `IDictionary<,>` → `"map"`
  - classe/record → `JsonObject` recursivo; tipo já na pilha → `"ref:{Nome}"`
- Nulável (`Nullable<T>` ou `NullabilityInfoContext` = `Nullable` no `AttributeProvider`) → sufixo `"?"` (ex.: `"string?"`, objeto aninhado vira `{ "$nullable": true, ... }`).
- `ParaJson`: chaves ordenadas, indentado.

- [ ] **Step 1: Testes do helper falhando** (`WireShapeTests`)
  - `WireShape_extrai_nomes_camelCase_com_opcoes_Http`: record `Exemplo(Guid Id, string Nome)` → chaves `["id","nome"]`.
  - `WireShape_marca_nulabilidade`: record `(string? A, int? B, string C)` → `"string?"`, `"number?"`, `"string"`.
  - `WireShape_respeita_JsonPropertyName`: `([property: JsonPropertyName("access_token")] string T)` → chave `"access_token"`.
  - `WireShape_enum_texto_vs_numero`: mesmo tipo com `OsEventos` → `"enum:string"`; com `PadraoDotNet` → `"enum:number"`.
  - `WireShape_inclui_propriedade_calculada`: `IntegrationEvent` derivado inclui `tipoEvento` e `correlationId`.
  - `WireJsonOptions_sao_read_only`: `IsReadOnly` true nas 3.

Run: `dotnet test tests/Fiap.TechChallenge.Contracts.Tests --filter WireShapeTests` — Expected: FAIL (tipos não existem).

- [ ] **Step 2: Implementar `WireJsonOptions`, `WireShape`, `WireFixture`** conforme Interfaces/Regras. Rodar — Expected: PASS.

- [ ] **Step 3: Ferramenta de captura (scratchpad)**
  - Console `net8.0`. `ProjectReference` para:
    - `os-microservice/src/OrdemServico.Lambda/OrdemServico.Lambda.csproj`
    - `fase1-tech-challenge/src/Fiap.TechChallenge.Api/Fiap.TechChallenge.Api.csproj`
    - `billing-service/src/Billing.Service.Api/Billing.Service.Api.csproj`
    - `execution-service/src/Fiap.TechChallenge.Execution.Api/Fiap.TechChallenge.Execution.Api.csproj`
    - `lamdba-auth/src/Fiap.TechChallenge.LambdaAuth/Fiap.TechChallenge.LambdaAuth.csproj`
  - `<Compile Include>` linkando `shared/tests/.../Support/WireShape.cs`.
  - Tabela de captura (nome da fixture → tipo do serviço → opções). Tipos privados/aninhados via `Type.GetType("Namespace.Classe+Aninhado, Assembly")`:
    - OS (`OsEventos`): `os.OrdemServicoCriada`, `os.StatusOrdemServicoAlterado`, `os.PecasInsumoIncluidas`, `os.OrcamentoReprovado`, `os.OrdemServicoCancelada`, `os.CompensacaoExecutada` (`Domain/Events/EventosDominio.cs`).
    - Core (`Http`): `core.ClienteResponse`, `core.VeiculoResponse`, `core.ServicoResponse`, `core.PecaInsumoResponse`, `core.ServicoMetricaTempoResponse`, `core.EstoqueResponse`, `core.PagedResult` (fechado em `VeiculoResponse`), `core.ClienteRequest`, `core.VeiculoRequest`, `core.VeiculoPatchRequest`, `core.ServicoRequest`, `core.PecaInsumoRequest`, `core.EstoqueRequest`, `core.LoginRequest`, `core.LoginResponse`, `core.CadastrarRequest`, `core.ErrorResponse` (`Middlewares/ExceptionHandlingMiddleware+ErrorResponse`).
    - Billing (`Http`): `billing.Orcamento` (entidade), `billing.ErrorResponse`.
    - Execution (`Http`): as 11 respostas de `Application/DTOs/ProducaoResponses.cs` como `execution.{Nome}`.
    - Auth (`PadraoDotNet`): `auth.AuthRequest`, `auth.AuthResponse`.
  - Fixtures de valores (não forma), gravadas no mesmo diretório:
    - `os.StatusOS.json`: `[{ id, codigo, descricao }]` lidos das propriedades estáticas de `StatusOS`.
    - `billing.PagamentoStatus.json`, `execution.StatusOrdemProducao.json`, `execution.StatusTarefaProducao.json`: `[{ nome, valor }]` via `Enum.GetValues`.
  - Saída: `shared/tests/Fiap.TechChallenge.Contracts.Tests/Fixtures/wire/`.

Run: `dotnet run --project <scratchpad>\wire-capture` — Expected: 1 arquivo por linha da tabela, nenhum `forma` vazio.

- [ ] **Step 4: Conferir 3 fixtures à mão** (`os.OrdemServicoCriada`, `core.ClienteResponse`, `auth.AuthResponse`) contra os arquivos-fonte. Expected: chaves batem com o código.

- [ ] **Step 5: Commit** — `test: add wire shape helper, json options catalog and captured wire fixtures`.

---

### Task 3: Common — corpos de erro e atributo `tipo`

**Files:**
- Create: `src/Fiap.TechChallenge.Contracts/Common/ErrorResponse.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/Common/SnsMessageAttributes.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/CommonWireTests.cs`

**Interfaces:**
- Produces (namespace `Fiap.TechChallenge.Contracts.Common`):
  - `public sealed record ErrorResponse(int StatusCode, string Message, string TraceId);` — remarks: usado por fase1, os-microservice e billing (`application/json`).
  - `public sealed record ProblemDetailsResponse(string? Type, string Title, int Status, string Detail, string Instance, string TraceId, string? CorrelationId);` — remarks: execution-service (`application/problem+json`), `type`/`correlationId` omitidos quando nulos.
  - `SnsMessageAttributes.TipoCompensacao = "tipo"` — remarks: atributo SQS da fila de compensação do OS (`SqsFilaCompensacao.cs`), sem `tipoEvento`/`origem`.

- [ ] **Step 1: Testes falhando**
  - `ErrorResponse_espelha_fase1_e_billing`: `WireFixture.DeveEspelhar<ErrorResponse>("core.ErrorResponse", WireJsonOptions.Http)` e idem `"billing.ErrorResponse"`.
  - `ProblemDetailsResponse_tem_chaves_do_execution`: chaves de `WireShape.Extrair(typeof(ProblemDetailsResponse), WireJsonOptions.Http)` = `type, title, status, detail, instance, traceId, correlationId`.
  - `SnsMessageAttributes_TipoCompensacao_deve_ser_tipo`.
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar** conforme Interfaces.
- [ ] **Step 4: Rodar** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts): mirror error bodies and compensation queue attribute`.

---

### Task 4: `OsService/DomainEvents` (eventos do outbox)

**Files:**
- Create: `src/Fiap.TechChallenge.Contracts/OsService/DomainEvents/EventosDominio.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/OsService/OrdemServicoEvents.cs` (remarks nos twins)
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/OsDomainEventsWireTests.cs`

**Interfaces:**
- Produces (namespace `Fiap.TechChallenge.Contracts.OsService.DomainEvents`), cópia exata de `os-microservice/src/OrdemServico.Lambda/Domain/Events/EventosDominio.cs:9-35`:
  - `public abstract record EventoDominio(Guid IdOrdemServico)` com `IdEvento: Guid = NewGuid`, `OcorridoEm: DateTime = UtcNow`, `TipoEvento => GetType().Name`, `CorrelationId: string? = CorrelationContext.Current`.
  - `OrdemServicoCriada(Guid IdOrdemServico, Guid IdCliente, Guid IdVeiculo, string Status)`
  - `StatusOrdemServicoAlterado(Guid IdOrdemServico, string StatusAnterior, string StatusAtual)`
  - `PecasInsumoIncluidas(Guid IdOrdemServico, Guid IdSaga, IReadOnlyCollection<Guid> PecasInsumosIds)`
  - `OrcamentoReprovado(Guid IdOrdemServico, IReadOnlyCollection<Guid> PecasInsumosIds)`
  - `OrdemServicoCancelada(Guid IdOrdemServico, string Motivo, IReadOnlyCollection<Guid> PecasInsumosIds)`
  - `CompensacaoExecutada(Guid IdOrdemServico, string IdCompensacao, string Tipo, string Origem)`
  - Remarks da base: publicado via outbox (`OrdemServicoRepository.cs:144-162`) com `WireJsonOptions.OsEventos`; `tipoEvento` sem sufixo `Event`; convive no mesmo tópico com `IntegrationEvent`.
- Remarks nos twins de `OrdemServicoEvents.cs` (`OrcamentoReprovadoEvent`, `PecasInsumoIncluidasEvent`, `StatusOrdemServicoAlteradoEvent`, `CompensacaoExecutadaEvent`): "Não trafega hoje: o OS publica `DomainEvents.{Nome}`."

- [ ] **Step 1: Testes falhando**
  - Um `[Theory]` `Evento_de_dominio_espelha_outbox_do_OS(string nome, Type tipo)` com as 6 linhas → `WireFixture.DeveEspelhar` com `WireJsonOptions.OsEventos` e fixture `os.{nome}`.
  - `TipoEvento_nao_tem_sufixo_Event`: `new OrcamentoReprovado(Guid.NewGuid(), []).TipoEvento.Should().Be("OrcamentoReprovado")`.
  - `Tipos_homonimos_ficam_em_sub_namespace`: os 6 tipos têm `Namespace == "Fiap.TechChallenge.Contracts.OsService.DomainEvents"`.
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar.**
- [ ] **Step 4: Rodar** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts): mirror os-microservice outbox domain events`.

---

### Task 5: OsService — status, rotas, resposta de criação, órfãos

**Files:**
- Modify: `src/Fiap.TechChallenge.Contracts/OsService/OrdemServicoStatus.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/OsService/OrdemServicoContracts.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/OsService/OrdemServicoEvents.cs`, `OrdemServicoModels.cs` (remarks)
- Modify: `tests/Fiap.TechChallenge.Contracts.Tests/OrdemServicoContractsTests.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/OsServiceWireTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record StatusOrdemServicoInfo(Guid Id, string Codigo, string Descricao);`
  - `OrdemServicoStatus.Catalogo: IReadOnlyList<StatusOrdemServicoInfo>` — 9 itens, valores copiados de `os-microservice/.../Domain/Entities/StatusOS.cs:23-31`. Constantes atuais mantidas; `APROVADA`/`REJEITADA` com remarks "Não trafega hoje: o OS não emite esse código."
  - `OrdemServicoApiRoutes.Health = "api/v1/ordens-servico/health"`, `OrdemServicoApiRoutes.ParametroAprovado = "aprovado"`.
  - `public sealed record OrdemServicoCriadaResponse(Guid Id, bool ClienteNotificado);` (resposta 201 de `POST api/v1/ordens-servico`, hoje objeto anônimo em `OrdemServicoEndpoints.cs:21`).
  - Remarks "Não trafega hoje": `DiagnosticoRejeitadoEvent`, `DiagnosticoFinalizadoEvent` (sem produtor), `ClienteContrato`, `VeiculoContrato`, `TarefaItemContrato` (sem uso). `ItemOrcamentoInfo`: só via `DiagnosticoFinalizadoEvent`.

- [ ] **Step 1: Testes falhando**
  - `Catalogo_de_status_espelha_StatusOS`: compara `Catalogo` com `os.StatusOS.json` (id, codigo, descricao, mesma ordem).
  - `Rotas_extras_do_OS`: `Health` e `ParametroAprovado` com os valores acima.
  - `OrdemServicoCriadaResponse_tem_chaves_id_e_clienteNotificado` (forma com `Http`).
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar; ajustar `OrdemServicoContractsTests` se algo existente quebrar.**
- [ ] **Step 4: Rodar suíte inteira** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts): mirror os status catalog, routes and create response`.

---

### Task 6: CoreService

**Files:**
- Modify: `src/Fiap.TechChallenge.Contracts/CoreService/CoreCatalogoContracts.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/CoreService/CoreEstoqueEvents.cs` (remarks)
- Modify: `tests/Fiap.TechChallenge.Contracts.Tests/CoreContractsTests.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/CoreWireTests.cs`

**Interfaces:**
- Produces (namespace `Fiap.TechChallenge.Contracts.CoreService`):
  - `ClienteCatalogoDto(Guid Id, string Nome, string CpfCnpj, string Email, string Celular)` — sai `Telefone`, entra `CpfCnpj`, `Celular` não-nulo (fonte `fase1-tech-challenge/src/Fiap.TechChallenge.Application/DTOs/Responses/ClienteResponse.cs`).
  - `ServicoCatalogoDto(Guid Id, string Nome, string Descricao, decimal ValorUnitario, int TempoEstimadoMinutos = 60)` (fonte `ServicoResponse.cs`).
  - `ServicoMetricaTempoDto(Guid IdServico, string Nome, int TempoEstimadoMinutos, int? TempoMedioExecucaoMinutos, int QuantidadeExecucoes)` (fonte `ServicoMetricaTempoResponse.cs`).
  - `IdCriadoResponse(Guid Id)` — resposta 201 de clientes, servicos, veiculos. Remarks: pecas-insumos, estoques e cadastro de staff devolvem guid cru.
  - `CoreApiRoutes`:
    - `LoginStaff = "api/v1/Autenticacao/Login"`, `CadastrarStaff = "api/v1/Autenticacao/Cadastrar"` (template exato).
    - Novas: `BuscarClientePorCpfCnpj = "api/v1/clientes/buscar-por-cpf-cnpj"`, `MetricasTempoServicos = "api/v1/servicos/metricas-tempo"`, `BuscarPecaInsumoPorDescricao = "api/v1/pecas-insumos/buscar-por-descricao"`, `QuantidadeEstoquePorPeca = "api/v1/estoques/quantidade/peca/{0}"`, `QuantidadeEstoquePorDescricao = "api/v1/estoques/quantidade/descricao"`, `RemoverEstoque = "api/v1/estoques/{0}"`, `Ping = "api/ping"`.
    - Remarks em `RemoverQuantidadeEstoque`/`AdicionarQuantidadeEstoque`: `{0}` é `idPecaInsumo`; body é `int` cru; sucesso devolve `text/plain`.
  - Remarks "Não trafega hoje": `AjustarQuantidadeEstoqueRequest` (fio é `int` cru), `EstoqueDetalhadoDto` (nenhum endpoint devolve), `ReservaEstoqueFalhouEvent` (Core não publica SNS; consumidor OS espera `ReservaEstoqueFalhou`).
  - Inalterados (fixture prova): `VeiculoCatalogoDto`, `PecaInsumoCatalogoDto`, `ClienteDetalhadoDto`, requests de criação/patch, `StaffLogin*`, `StaffCadastrarRequest`, `PagedResultContrato<T>`.

- [ ] **Step 1: Testes falhando** — `[Theory]` `Dto_do_Core_espelha_fase1(string fixture, Type tipo)` com `Http`:
  - `core.ClienteResponse` → `ClienteCatalogoDto` e `ClienteDetalhadoDto`
  - `core.VeiculoResponse` → `VeiculoCatalogoDto`
  - `core.ServicoResponse` → `ServicoCatalogoDto`
  - `core.PecaInsumoResponse` → `PecaInsumoCatalogoDto`
  - `core.ServicoMetricaTempoResponse` → `ServicoMetricaTempoDto`
  - `core.EstoqueResponse` → `EstoqueDetalhadoDto`
  - `core.PagedResult` → `PagedResultContrato<VeiculoCatalogoDto>`
  - `core.ClienteRequest` → `CriarClienteRequest` e `AtualizarClienteRequest`
  - `core.VeiculoRequest` → `CriarVeiculoRequest`; `core.VeiculoPatchRequest` → `AtualizarVeiculoPatchRequest`
  - `core.ServicoRequest` → `CriarServicoRequest`; `core.PecaInsumoRequest` → `CriarPecaInsumoRequest`; `core.EstoqueRequest` → `CriarEstoqueRequest`
  - `core.LoginRequest` → `StaffLoginRequest`; `core.LoginResponse` → `StaffLoginResponse`; `core.CadastrarRequest` → `StaffCadastrarRequest`
  - Mais `Rotas_do_Core_espelham_controllers` com todos os valores de `CoreApiRoutes` acima.
- [ ] **Step 2: Rodar** — Expected: FAIL em `ClienteCatalogoDto`, `ServicoCatalogoDto`, `ServicoMetricaTempoDto`, rotas.
- [ ] **Step 3: Implementar; atualizar `CoreContractsTests` (construtores posicionais e rotas Login/Cadastrar).**
- [ ] **Step 4: Rodar suíte inteira** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts)!: mirror core catalog dtos and routes from fase1`.

---

### Task 7: BillingService

**Files:**
- Modify: `src/Fiap.TechChallenge.Contracts/BillingService/BillingModels.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/BillingService/BillingContracts.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/BillingService/BillingEvents.cs` (remarks)
- Modify: `tests/Fiap.TechChallenge.Contracts.Tests/BillingContractsTests.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/BillingWireTests.cs`

**Interfaces:**
- Produces (namespace `Fiap.TechChallenge.Contracts.BillingService`):
  - `StatusPagamentoContrato { Pendente = 0, Aprovado = 1, Rejeitado = 2, Cancelado = 3, Expirado = 4 }` (fonte `billing-service/src/Billing.Service.Domain/Entities/Pagamento.cs:5-12`). Remarks: no fio aparece como texto minúsculo em `BoletoResponse.Status`.
  - `MetodoPagamentoContrato`: inalterado, remarks "Não trafega hoje: billing usa `MeioPagamento` string `\"boleto\"`."
  - `CriarBoletoRequest(Guid IdOrcamento, string Email, string Nome, string Sobrenome, string Cpf, EnderecoBoletoRequest Endereco)` — `Endereco` obrigatório (billing devolve 400 sem ele, `PagamentoService.cs:25-27`).
  - `OrcamentoResponse(Guid Id, Guid IdOrdemServico, Guid IdOrcamento, ValorMonetarioResponse ValorTotal, DateTime CriadoEm, DateTime AtualizadoEm, DateTime? ApagadoEm)` e `ValorMonetarioResponse(decimal Valor)` — fonte entidade `Orcamento` serializada pelos endpoints de orçamento.
  - Webhook Mercado Pago:
    - `MercadoPagoWebhookRequest(string? Action, string? Type, MercadoPagoWebhookData? Data, string? Id = null, string? Status = null)`
    - `MercadoPagoWebhookData(string? Id, string? Status = null)`
    - `Id` e `Status` com `[property: JsonConverter(typeof(TextoOuNumeroJsonConverter))]` (aceita string ou número, lê número como texto cru).
    - `internal sealed class TextoOuNumeroJsonConverter : JsonConverter<string?>` em `BillingService/TextoOuNumeroJsonConverter.cs`.
    - `public static class MercadoPagoWebhookParametros { HeaderAssinatura = "x-signature"; HeaderRequestId = "x-request-id"; ChaveTimestamp = "ts"; ChaveAssinatura = "v1"; QueryDataId = "data.id"; QueryId = "id"; FormatoManifesto = "id:{0};request-id:{1};ts:{2};"; }`.
    - Remarks: precedência do id (`data.id` → `data` escalar → `id` raiz → query `data.id` → query `id`); `data` escalar não é modelado pelo record; resposta sempre 200 vazio; 401 sem corpo em assinatura inválida.
  - `BillingApiRoutes`: novas `DecisaoOrcamento = "api/v1/orcamento/{0}/decisao"`, `ParametroAprovado = "aprovado"`, `ParametroIdOrdemServico = "idOrdemServico"`, `Ping = "api/ping"`. Remarks: `ListarOrcamentos` devolve array simples (404 se vazio); `GerarOrcamento` recebe `?idOrdemServico`.
  - Remarks em eventos: `PagamentoRecusadoEvent` "Não trafega hoje: billing publica `PagamentoCanceladoCompensacaoEvent`."; `OrcamentoGeradoEvent.LinkPagamento` aponta para `api/v1/pagamento/checkout/{id}`, rota que não existe; `PagamentoEstornadoEvent.PagamentoId` pode ser `Guid.Empty`.

- [ ] **Step 1: Testes falhando**
  - `StatusPagamentoContrato_espelha_valores_do_billing`: nomes e ints iguais a `billing.PagamentoStatus.json`.
  - `OrcamentoResponse_espelha_entidade_Orcamento`: `DeveEspelhar<OrcamentoResponse>("billing.Orcamento", Http)`.
  - `Webhook_le_id_numerico_como_texto`: `{"data":{"id":123}}` → `Data.Id == "123"`.
  - `Webhook_le_id_texto`: `{"data":{"id":"abc"},"status":"approved"}` → `Data.Id == "abc"`, `Status == "approved"`.
  - `Webhook_le_id_na_raiz`: `{"id":"9"}` → `Id == "9"`.
  - `Parametros_do_webhook` e `Rotas_do_billing` com os valores acima.
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar; atualizar `BillingContractsTests` (enum e construtor de `CriarBoletoRequest`).**
- [ ] **Step 4: Rodar suíte inteira** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts)!: mirror billing payment status, quote response and mercado pago webhook`.

---

### Task 8: ExecutionService

**Files:**
- Create: `src/Fiap.TechChallenge.Contracts/ExecutionService/Responses/ProducaoResponses.cs`
- Create: `src/Fiap.TechChallenge.Contracts/ExecutionService/StatusProducao.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/ExecutionService/ExecutionContracts.cs`
- Modify: `src/Fiap.TechChallenge.Contracts/ExecutionService/ExecutionModels.cs` (remarks)
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/ExecutionWireTests.cs`

**Interfaces:**
- Produces:
  - Namespace `Fiap.TechChallenge.Contracts.ExecutionService.Responses`: cópia exata dos 11 records de `execution-service/src/Fiap.TechChallenge.Execution.Application/DTOs/ProducaoResponses.cs` (`OrdemProducaoResponse`, `ClienteSnapshotResponse`, `VeiculoSnapshotResponse`, `DiagnosticoResponse`, `ServicoSugeridoResponse`, `PecaSugeridaResponse`, `TarefaProducaoResponse`, `HistoricoProducaoResponse`, `OrdemProducaoFilaItemResponse`, `RelatorioProdutividadeResponse`, `MetricaProdutividadeMecanicoResponse`), mesmos nomes, tipos, nulabilidade e ordem.
  - Namespace `Fiap.TechChallenge.Contracts.ExecutionService`:
    - `StatusOrdemProducaoContrato`: consts `AGUARDANDO_DIAGNOSTICO`, `EM_DIAGNOSTICO`, `DIAGNOSTICO_CONCLUIDO`, `NA_FILA`, `EM_EXECUCAO`, `CONCLUIDA`, `CANCELADA` (valor = nome).
    - `StatusTarefaProducaoContrato`: consts `PENDENTE`, `EM_EXECUCAO`, `CONCLUIDA`, `INTERROMPIDA`.
    - Remarks: valores numéricos do domínio (1..7, 1..4) aceitos no filtro `?status=` de `fila`.
  - `ExecutionApiRoutes`: novas `ParametroStatus = "status"`, `ParametroFilialId = "filialId"`, `ParametroDe = "de"`, `ParametroAte = "ate"`, `ParametroIdMecanico = "idMecanico"`. Remarks de verbo: `IniciarDiagnostico`, `IniciarTarefa`, `FinalizarTarefa` são `PATCH`.
  - Remarks em `ServicoSugeridoInfo`/`PecaSugeridaInfo`: serializados pelo tipo declarado.

- [ ] **Step 1: Testes falhando**
  - `[Theory]` `Resposta_do_execution_espelha_servico(string nome, Type tipo)` com as 11 → `DeveEspelhar` fixture `execution.{nome}`, `Http`.
  - `Status_de_producao_espelha_enum`: nomes de `execution.StatusOrdemProducao.json` convertidos para UPPER_SNAKE = consts de `StatusOrdemProducaoContrato`; idem tarefa.
  - `Tipos_homonimos_ficam_em_sub_namespace`: os 11 em `...ExecutionService.Responses`.
  - `Parametros_de_query_do_execution`.
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar.**
- [ ] **Step 4: Rodar suíte inteira** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts): mirror execution-service http responses and status codes`.

---

### Task 9: AuthService

**Files:**
- Modify: `src/Fiap.TechChallenge.Contracts/AuthService/AuthContracts.cs`
- Modify: `tests/Fiap.TechChallenge.Contracts.Tests/AuthContractsTests.cs`
- Test: `tests/Fiap.TechChallenge.Contracts.Tests/Wire/AuthWireTests.cs`

**Interfaces:**
- Produces (namespace `Fiap.TechChallenge.Contracts.AuthService`):
  - `AuthTokenResponse([property: JsonPropertyName("access_token")] string AccessToken, [property: JsonPropertyName("token_type")] string TokenType, [property: JsonPropertyName("expires_in")] int ExpiresIn)` — sem defaults (fonte `lamdba-auth/.../Models/AuthResponse.cs`).
  - `AuthErrorResponse([property: JsonPropertyName("error")] string Error)` — corpo 400/401/500 do lambda-auth.
  - `JwtAuthConstants.Claims`: novas `Sub = "sub"`, `Jti = "jti"`. Remarks: lambda-auth emite `sub` (id do cliente), `jti`, `cpf`, sem role e sem `UsuarioId`; fase1 emite `UsuarioId`, `ClaimTypes.Name`, `ClaimTypes.Email`, `ClaimTypes.Role`.
  - `JwtAuthConstants.LambdaAuth`: `Rota = "auth/token"`, `IssuerPadrao = "fiap-tech-challenge"`, `AudiencePadrao = "fiap-api"`, `ExpiracaoPadraoSegundos = 3600`, `EnvIssuer = "JWT_ISSUER"`, `EnvAudience = "JWT_AUDIENCE"`, `EnvSecret = "JWT_SECRET"`, `EnvExpiracao = "JWT_EXPIRES_IN_SECONDS"`.
  - Remarks em `DefaultIssuer`/`DefaultAudience`: divergem de `LambdaAuth.IssuerPadrao`/`AudiencePadrao` (também em `soat-infra/environments/dev/main.tf:73-74`); token do lambda-auth falha validação em OS/execution como configurados.
  - `AuthTokenRequest`: inalterado.

- [ ] **Step 1: Testes falhando**
  - `AuthTokenResponse_espelha_lambda_auth`: `DeveEspelhar<AuthTokenResponse>("auth.AuthResponse", PadraoDotNet)`.
  - `AuthTokenRequest_espelha_lambda_auth`: `DeveEspelhar<AuthTokenRequest>("auth.AuthRequest", PadraoDotNet)`.
  - `AuthErrorResponse_serializa_chave_error`: `JsonSerializer.Serialize(new AuthErrorResponse("x"))` == `{"error":"x"}`.
  - `Constantes_do_lambda_auth` com os valores acima.
- [ ] **Step 2: Rodar** — Expected: FAIL.
- [ ] **Step 3: Implementar; atualizar `AuthContractsTests` (defaults removidos).**
- [ ] **Step 4: Rodar suíte inteira** — Expected: PASS.
- [ ] **Step 5: Commit** — `feat(contracts)!: mirror lambda-auth token response, error body and jwt defaults`.

---

### Task 10: Documentação

**Files:**
- Modify: `README.md` (seção de inventário de contratos)
- Modify: `RELATORIO_AUDITORIA_CONTRATOS.md`, `ARQUITETURA_E_VALIDACAO_FASE4.md` (aviso no topo)

- [ ] **Step 1: README** — substituir inventário por tabela `área | tipo | fonte da verdade (arquivo) | trafega? | observação`, gerada a partir dos tipos atuais do `src`. Incluir subseção "Como regenerar fixtures" (descrevendo a ferramenta de captura e a tabela da Task 2, Step 3) e "Divergências conhecidas" (issuer/audience, dois envelopes no tópico, nomes `ReservaEstoqueFalhou`/`PagamentoRecusado`, link de checkout inexistente).
- [ ] **Step 2: Aviso nos relatórios** — primeira linha: `> ⚠️ Desatualizado em 2026-11-08: afirmações de "zero duplicação" e "100% conforme" não refletem o código. Ver docs/superpowers/specs/2026-11-08-espelho-contratos-design.md.`
- [ ] **Step 3: Commit** — `docs: rewrite contract inventory and flag outdated audit reports`.

---

### Task 11: Verificação final e descrição do PR

**Files:**
- Create (fora do repo): `<scratchpad>\pr-espelho-contratos.md`

- [ ] **Step 1: Suíte completa** — `dotnet test tests/Fiap.TechChallenge.Contracts.Tests -c Release`. Expected: 0 falhas.
- [ ] **Step 2: Recapturar fixtures e comparar** — rodar a ferramenta de captura de novo (só os serviços que compilam) e `git diff --stat tests/Fiap.TechChallenge.Contracts.Tests/Fixtures`. Expected: sem diff.
- [ ] **Step 3: Build dos serviços contra o shared novo** — `dotnet build` em `billing-service/Billing.Service.sln`, `execution-service/Fiap.TechChallenge.Execution.sln`, `fase1-tech-challenge/Fiap.TechChallenge.Api.slnx`, `os-microservice/OrdemServico.Lambda.sln`. Registrar cada erro `arquivo:linha — código — mensagem`.
- [ ] **Step 4: Descrição do PR** — resumo por área, lista de quebras do Step 3, consequência (CI dos serviços vermelho até ajuste), divergências conhecidas. Mostrar ao usuário; push e PR só com ok.
