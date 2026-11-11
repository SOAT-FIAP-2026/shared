# Espelho de contratos no `shared` — design

- **Data:** 2026-11-08
- **Repo:** `shared` (`Fiap.TechChallenge.Contracts`)
- **Branch:** `feat/espelho-contratos`
- **Status:** design aprovado; spec aguardando revisão

## 1. Objetivo

O `shared` é a referência central dos contratos de integração. Hoje ele diverge do que os serviços realmente trocam (auditoria de 2026-11-08: twins locais, envelopes paralelos, DTOs HTTP com outro formato, rotas sem uso). Esta mudança atualiza o `shared` para **espelhar exatamente** os contratos em uso hoje nos serviços.

Direção: **serviços → `shared`**. Nenhum serviço é alterado.

## 2. Decisões já tomadas

| Tema | Decisão |
|---|---|
| Compatibilidade | Espelho exato. Pode quebrar o build dos serviços que usam tipos alterados. |
| Ajuste dos serviços | Fora de escopo. Só o `shared` muda. Quebras são listadas no PR, não corrigidas. |
| Contratos órfãos / sem uso | Mantidos. Recebem `<remarks>` "Não trafega hoje" (sem `[Obsolete]`, para não gerar warning). |
| Abordagem | Reescrever no lugar, guiado pelo código dos serviços, com verificação por comparador local. |

Consequência aceita: os serviços fazem checkout de `shared/main` sem versão fixa, então o CI deles fica vermelho após o merge até alguém ajustar.

## 3. Regras de espelhamento

1. **Evento (SNS/SQS):** fonte da verdade = tipo que o **publicador** serializa. O nome da classe no `shared` é igual ao valor de `tipoEvento` no fio (`TipoEvento => GetType().Name`).
2. **HTTP:** fonte da verdade = DTO que o controller/endpoint do **provedor** recebe ou devolve. O nome da classe do `shared` é mantido (não vai no fio); propriedades, tipos e nulabilidade passam a bater.
3. **Nome C# ≠ nome no fio:** usar `[JsonPropertyName]`.
4. **Enum:** espelhar membros e valores numéricos do provedor. Como é serializado (int ou texto) fica documentado em `WireJsonOptions` (seção 4.6).
5. **Dois formatos reais para o mesmo fato** (ex.: `OrdemServicoCanceladaEvent` publicado direto e `OrdemServicoCancelada` via outbox): os dois existem no `shared`, cada um documentando quem publica.
6. **Divergência que só se resolve mudando serviço** (ex.: issuer/audience JWT): registrar no XML doc e no README; não escolher lado.
7. **Contrato sem produtor ou sem uso:** fica, com `<remarks>Não trafega hoje: …</remarks>` explicando o motivo.

## 4. Mudanças por área

Fonte da verdade de cada área (caminhos relativos a `D:\Projetos\FIAP\`).

### 4.1 `OsService/DomainEvents/` (novo)

- Fonte: `os-microservice/src/OrdemServico.Lambda/Domain/Events/EventosDominio.cs` e o outbox em `Infrastructure/Persistence/OrdemServicoRepository.cs`.
- Conteúdo: base `EventoDominio` (`IdOrdemServico`, `IdEvento`, `OcorridoEm`, `TipoEvento`, `CorrelationId`) e os 6 eventos: `OrdemServicoCriada`, `StatusOrdemServicoAlterado`, `PecasInsumoIncluidas`, `OrcamentoReprovado`, `OrdemServicoCancelada`, `CompensacaoExecutada`.
- Namespace próprio `Fiap.TechChallenge.Contracts.OsService.DomainEvents`, que nenhum serviço importa hoje (evita ambiguidade com os tipos locais do OS).
- Os twins atuais do `shared` (`OrcamentoReprovadoEvent`, `PecasInsumoIncluidasEvent`, `StatusOrdemServicoAlteradoEvent`, `CompensacaoExecutadaEvent`) ficam, com remarks apontando para o tipo de `DomainEvents` que de fato trafega.

### 4.2 `OsService/` (eventos `IntegrationEvent`, HTTP, status)

- Fonte eventos: publicações diretas em `os-microservice/.../Application/Services/OrdemServicoService.cs`, `Application/Saga/CompensacaoService.cs`, `Infrastructure/Messaging/OsSqsBackgroundWorker.cs`.
- Fonte HTTP: `os-microservice/.../Api/OrdemServicoEndpoints.cs` e DTOs que ele binda.
- `OrdemServicoApiRoutes` = rotas reais dos endpoints.
- `OrdemServicoStatus` = mesmos códigos de `os-microservice/.../Domain/Entities/StatusOS.cs`.
- `ComandoCompensacaoContrato` / `TipoCompensacaoContrato`: conferir contra `SqsFilaCompensacao.cs` e `CompensacaoFilaFunction.cs`.

### 4.3 `CoreService/`

- Fonte: controllers `fase1-tech-challenge/src/Fiap.TechChallenge.Api/Controllers/V1/*` e DTOs em `Fiap.TechChallenge.Application/DTOs/{Requests,Responses,Common}/*`.
- DTOs de catálogo (`ClienteCatalogoDto`, veículo, serviço, peça/insumo, estoque) passam a ter o formato dos `*Response` / `*Request` reais (ex.: `CpfCnpj`, `Celular`).
- Paginação: espelhar `PagedRequest` / `PagedResult` do fase1.
- `CoreApiRoutes` = rotas reais.
- Estoque: o endpoint recebe `int` cru no body (`EstoqueController`). `AjustarQuantidadeEstoqueRequest` fica, com remarks dizendo que o fio é um `int`.
- `ReservaEstoqueFalhouEvent`: sem produtor (Core não publica SNS) → remarks.

### 4.4 `ExecutionService/`

- Fonte eventos: use cases que publicam em `execution-service/src/Fiap.TechChallenge.Execution.Application/` via `AwsSnsEventPublisher`.
- Fonte HTTP: `Api/Controllers/V1/ProducaoController.cs` e `Application/DTOs/ProducaoResponses.cs`.
- Status: `StatusOrdemProducao`, `StatusTarefaProducao` do domínio.
- `ExecutionApiRoutes` = rotas reais.

### 4.5 `BillingService/`

- Fonte eventos: `billing-service/src/Billing.Service.Application/Services/{OrcamentoService,PagamentoService}.cs`, `Infrastructure/Messaging/BillingSqsBackgroundWorker.cs`.
- Fonte HTTP: `Api/Controllers/V1/{OrcamentoController,PagamentoController}.cs`, `Application/DTOs/*`.
- Status: `PagamentoStatus` (`Domain/Entities/Pagamento.cs`) e `StatusOS` local (`Domain/Entities/StatusOrdemServico.cs`).
- `MercadoPagoWebhookRequest` / `MercadoPagoWebhookData`: exatamente os campos que `PagamentoController` lê do `JsonElement`.
- `BillingApiRoutes` = rotas reais.
- `PagamentoRecusadoEvent`, `DiagnosticoFinalizadoEvent`: sem produtor → remarks.

### 4.6 `Common/`

- `ErrorResponse`: espelho do corpo de erro de `fase1-tech-challenge/.../Middlewares/ExceptionHandlingMiddleware.cs`, `os-microservice/.../Api/Middlewares.cs` e `billing-service/.../Middlewares/ExceptionHandlingMiddleware.cs` (se os três divergirem, documentar cada variante).
- `WireJsonOptions` (novo): duas configurações em uso, só como referência documentada:
  - `OsService`: `JsonSerializerDefaults.Web` + `JsonStringEnumConverter` + `WhenWritingNull` (cópia de `os-microservice/.../Configuration/JsonDefaults.cs`).
  - `DotNetDefault`: opções padrão do `System.Text.Json` (billing e execution publicam assim).
- `SnsMessageAttributes`: incluir a chave `tipo` usada pela fila de compensação do OS (`SqsFilaCompensacao.cs`).
- `IntegrationEvent`, `CorrelationContext`, `SnsCorrelationHelper`: sem mudança de formato.

### 4.7 `AuthService/`

- Fonte: `lamdba-auth/src/Fiap.TechChallenge.LambdaAuth/{Models,Services,Function.cs}`.
- `AuthTokenRequest` / `AuthTokenResponse`: formato do lambda-auth, com `[JsonPropertyName]` snake_case (`access_token`, `token_type`, `expires_in`).
- `JwtAuthConstants.Claims`: claims que o lambda-auth emite (`cpf`) e as que os serviços leem (`UsuarioId`, roles).
- Issuer/audience: documentar a divergência (`fiap-tech-challenge`/`fiap-api` no lambda-auth e em `soat-infra/environments/dev/main.tf` vs `TechChallenge`/`techchallenge.com.br` no resto). Constantes atuais mantidas.

### 4.8 Pacote, CI e docs

- `Version` no csproj: `2.0.0`. Sem tag, sem publicação.
- `.github/workflows/ci-publish.yml`: `dotnet test` só em `tests/Fiap.TechChallenge.Contracts.Tests`. E2E vai para workflow novo `e2e.yml` com `workflow_dispatch`.
- `README.md`: seção de inventário de contratos reescrita conforme o novo estado.
- `RELATORIO_AUDITORIA_CONTRATOS.md` e `ARQUITETURA_E_VALIDACAO_FASE4.md`: aviso no topo de que estão desatualizados, apontando para esta spec.

## 5. Verificação

1. **Ambiente:** instalar .NET 8 SDK via `winget install Microsoft.DotNet.SDK.8`.
2. **Testes do `shared`:** um teste por contrato que trafega, serializando com as opções do publicador (`WireJsonOptions`) e comparando com JSON esperado. Testes existentes ajustados ao novo formato.
3. **Comparador de paridade (descartável, fora do repo):** console no scratchpad que referencia cada serviço e o `shared`, serializa o tipo local e o tipo do `shared` com as mesmas opções e compara nomes de propriedade e tipos JSON. Resultado resumido na descrição do PR.
4. **Build dos serviços:** `dotnet build` de `billing-service`, `execution-service`, `fase1-tech-challenge`, `os-microservice` contra o `shared` novo (referência relativa `../shared`). Lista de erros (arquivo:linha) na descrição do PR, sem correção.
5. **Critério de pronto:**
   - Todo contrato que trafega tem tipo no `shared` com formato idêntico ao do fio (comparador sem diferença).
   - `Contracts.Tests` verde; CI do `shared` verde.
   - PR descreve quebras esperadas nos serviços.

## 6. Fora de escopo

- Alterar qualquer serviço, infra (`soat-infra`, `template.yaml`, floci) ou filter policies.
- Corrigir bugs da saga achados na auditoria (estorno duplo, publish duplo, casing na compensação).
- Publicar pacote NuGet ou fixar versão nos serviços.
- Unificar envelopes, nomes de evento ou opções de JSON (isso é a fase seguinte, serviços → padrão do `shared`).

## 7. Riscos

- **Fidelidade:** o espelho é feito lendo código; o comparador de paridade mitiga. Contratos montados à mão (ex.: `JsonElement` no webhook, `JsonDocument` na compensação) não têm tipo local para comparar — revisão manual.
- **CI dos serviços vermelho após merge:** aceito; mitigação futura é fixar versão do `shared` nos serviços.
- **Restauração de pacotes:** instalar SDK e restaurar NuGet exige rede; sem isso a verificação 3–4 não roda.
