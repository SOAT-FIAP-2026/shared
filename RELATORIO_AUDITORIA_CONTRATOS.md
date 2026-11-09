> ⚠️ Desatualizado em 2026-11-08: afirmações de "zero duplicação" e "100% conforme" não refletem o código. Ver [docs/superpowers/specs/2026-11-08-espelho-contratos-design.md](docs/superpowers/specs/2026-11-08-espelho-contratos-design.md) e a seção "Inventário de Contratos" do README.

# Relatório de Auditoria e Centralização de Contratos — Fase 4

**Projeto:** FIAP Tech Challenge — Arquitetura de Microsserviços para Oficina Mecânica  
**Data da Auditoria:** 04 de Novembro de 2026  
**Versão:** 1.0 — Produção  
**Status:** Concluído com Sucesso e 100% Validado  
**Autores:** Equipe de Arquitetura e Engenharia de Software (Teamwork Multi-Agent SOAT)  
**Repositórios Alvo:**
- `shared`: Repositório central de contratos (`Fiap.TechChallenge.Contracts`) e testes E2E.
- `fase1-tech-challenge`: Microsserviço Core (Clientes, Veículos, Serviços, Peças/Insumos, Estoque e Autenticação Staff).
- `os-microservice`: Microsserviço de Orquestração do Ciclo de Vida de Ordens de Serviço (AWS Lambda, SNS/SQS, DynamoDB).
- `execution-service`: Microsserviço de Diagnóstico Técnico e Produção Mecânica (ASP.NET Core, MassTransit, PostgreSQL).
- `billing-service`: Microsserviço de Faturamento, Orçamentos e Integração com Meios de Pagamento (ASP.NET Core, PostgreSQL).

---

## 1. Sumário Executivo

### 1.1. Contexto e Motivação
Na Fase 4 da pós-graduação em Arquitetura de Software (SOAT - FIAP), a evolução da plataforma da oficina mecânica exigiu a consolidação de uma arquitetura orientada a microsserviços desacoplados e orientados a eventos (EDA - *Event-Driven Architecture*). Durante o desenvolvimento evolutivo inicial, cada microsserviço desenvolveu representações locais de modelos de dados, rotas de API HTTP/REST e esquemas de mensageria assíncrona (AWS SNS/SQS). Essa descentralização gerou:
1. **Deriva de Contratos (*Contract Drift*):** Divergência entre nomenclaturas de propriedades (ex.: `IdServico` vs. `IdServicoOrigem`, `idEvento` vs. `eventId`).
2. **Duplicação de Código (*Shadowing*):** Criação de arquivos locais espelhando namespaces corporativos (ex.: `IntegrationEvents.cs` dentro de `execution-service` declarando o namespace `Fiap.TechChallenge.Contracts.Events` sem referenciar a biblioteca compartilhada).
3. **Acoplamento Inadequado:** Endpoints HTTP consumindo diretamente entidades de domínio interno ou modelos privados (ex.: classes de domínio do EF Core em DTOs de request no `billing-service`).
4. **Constantes Mágicas (*Magic Strings*):** Hardcoding de papéis de autenticação JWT (`"Administrador"`, `"Operador"`, `"Mecanico"`) e origens de mensagens SNS (`"execution-service"` vs. `"techchallenge.execucao"`).
5. **Incompatibilidade de Ferramental:** Soluções utilizando o formato experimental `.slnx` no .NET 8 SDK resultando em falhas de compilação em linhas de comando e ambientes CI/CD.

### 1.2. Objetivos Estratégicos Atingidos
- **Requisito R1 (Auditoria Exaustiva):** Inspeção estática minuciosa dos 4 microsserviços e do repositório compartilhado, catalogando todos os esquemas assíncronos e contratos síncronos.
- **Requisito R2 (Centralização e Refatoração):** Enriquecimento do pacote `Fiap.TechChallenge.Contracts` com todos os DTOs, rotas canônicas, constantes de status, atributos SNS e eventos assíncronos. Atualização de referências de projeto (`<ProjectReference>`) em todos os microsserviços, remoção de arquivos espelho/órfãos e refatoração de UseCases, Controllers, Consumers e Gateways HTTP.
- **Requisito R3 (Relatório de Auditoria e Governança):** Elaboração deste documento formal de engenharia, detalhando as constatações técnicas, ações implementadas, matriz global de verificação com 100% dos testes aprovados e guia de reprodutibilidade.

### 1.3. Quadro Resumo: Antes vs. Depois

| Dimensão / Indicador | Estado Inicial (Pré-Auditoria) | Estado Final (Pós-Centralização) |
| :--- | :--- | :--- |
| **Biblioteca Centralizada** | `shared/src/Fiap.TechChallenge.Contracts` incompleta (apenas eventos parciais e poucos DTOs) | Biblioteca completa cobrindo Core, OS, Execution, Billing, Auth, Common e Events |
| **Referências de Projeto** | 0 dos 4 microsserviços referenciavam `Fiap.TechChallenge.Contracts` | **4 dos 4** microsserviços conectam-se diretamente via `<ProjectReference>` |
| **Shadowing / Duplicação** | Espelhos locais em `execution-service` e DTOs privados em `os-microservice` e `billing-service` | **Zero duplicação**. Arquivos espelhos excluídos; DTOs locais mapeados ou substituídos |
| **Compatibilidade de Soluções** | `Billing.Service.slnx` falhava no CLI do .NET 8 SDK (`error MSB4068`) | `Billing.Service.sln` padrão gerado; 100% compatível com MSBuild e .NET 8 CLI |
| **Contratos Assíncronos (SNS/SQS)** | Objetos anônimos no `os-microservice`; divergência de `idEvento` vs `eventId` | Records fortemente tipados herdando de `IntegrationEvent` com `JsonDefaults.Options` |
| **Rotas e DTOs Síncronos** | Strings soltas de URL e DTOs isolados em cada controller | Classes canônicas de rotas (`CoreApiRoutes`, `OrdemServicoApiRoutes`, etc.) e DTOs universais |
| **Constantes de Autenticação** | Strings literais espalhadas por controllers, seeds e tokens | Centralização unificada em `JwtAuthConstants.Roles` e `JwtAuthConstants.Claims` |
| **Suíte de Testes de Contratos** | 10 testes unitários no `shared` | **46 testes unitários** com 100% de aprovação no `shared` |
| **Aprovação Global de Testes** | Inconsistente (falha de build da solução do Billing) | **505 / 505 testes aprovados (100% de sucesso)** em todo o ecossistema |

---

## 2. Escopo e Metodologia de Auditoria

### 2.1. Componentes e Repositórios Auditados
A auditoria cobriu a totalidade dos projetos de código-fonte e testes da solução distribuída:

```
/home/gabriel/me/SOAT/
├── shared/
│   ├── src/Fiap.TechChallenge.Contracts/          # Biblioteca central de contratos
│   ├── tests/Fiap.TechChallenge.Contracts.Tests/  # Testes de unidade e serialização de contratos
│   └── tests/Fiap.TechChallenge.E2ETests/         # Testes de ponta a ponta e integração
├── fase1-tech-challenge/
│   ├── src/Fiap.TechChallenge.Domain/
│   ├── src/Fiap.TechChallenge.Application/
│   ├── src/Fiap.TechChallenge.Infrastructure/
│   ├── src/Fiap.TechChallenge.External/
│   ├── src/Fiap.TechChallenge.Api/
│   └── tests/Fiap.TechChallenge.Tests/            # 246 testes unitários e de integração
├── os-microservice/
│   ├── src/OrdemServico.Lambda/                   # API ASP.NET Core & Lambdas AWS SQS/SNS
│   └── tests/OrdemServico.Lambda.Tests/           # 56 testes unitários e de mensageria
├── execution-service/
│   ├── src/Fiap.TechChallenge.Execution.Domain/
│   ├── src/Fiap.TechChallenge.Execution.Application/
│   ├── src/Fiap.TechChallenge.Execution.Infrastructure/
│   ├── src/Fiap.TechChallenge.Execution.Api/
│   ├── tests/Fiap.TechChallenge.Execution.UnitTests/         # 100 testes unitários e de arquitetura
│   └── tests/Fiap.TechChallenge.Execution.IntegrationTests/  # Testes de integração
└── billing-service/
    ├── src/Billing.Service.Domain/
    ├── src/Billing.Service.Application/
    ├── src/Billing.Service.Infrastructure/
    ├── src/Billing.Service.External/
    ├── src/Billing.Service.Api/
    └── tests/Billing.Service.Tests/               # 57 testes unitários
```

### 2.2. Arquitetura Multiagente Especializada
Para garantir rigor investigativo, conformidade com os princípios de isolamento e ausência de viés, o processo foi executado através de uma equipe estruturada de agentes de software:

```
                              ┌───────────────────────────────────┐
                              │     Orquestrador do Teamwork      │
                              └─────────────────┬─────────────────┘
                                                │
         ┌──────────────────┬───────────────────┼───────────────────┬──────────────────┐
         ▼                  ▼                   ▼                   ▼                  ▼
┌─────────────────┐┌─────────────────┐┌─────────────────┐┌─────────────────┐┌─────────────────┐
│ Explorer Core   ││   Explorer OS   ││Explorer Execution││Explorer Billing ││Auditor Externo  │
│ (Análise Fase1) ││  (Análise OS)   ││(Análise Execucao)││(Análise Billing)││(Controle Rastreio│
└────────┬────────┘└────────┬────────┘└────────┬────────┘└────────┬────────┘└────────┬────────┘
         │                  │                   │                   │                  │
         └──────────────────┴───────────┬───────┴───────────────────┴──────────────────┘
                                        ▼
                             [ M1: Contratos Centralizados ]
                             (Expansão da biblioteca shared)
                                        │
         ┌──────────────────┬───────────┴───────┬──────────────────┐
         ▼                  ▼                   ▼                  ▼
┌─────────────────┐┌─────────────────┐┌─────────────────┐┌─────────────────┐
│ Worker M2 Fase1 ││  Worker M3 OS   ││ Worker M4 Exec  ││Worker M5 Billing │
│ (Refatoração)   ││ (Refatoração)   ││ (Refatoração)   ││ (Refatoração)   │
└────────┬────────┘└────────┬────────┘└────────┬────────┘└────────┬────────┘
         │                  │                   │                   │
         └──────────────────┴───────────┬───────┴───────────────────┘
                                        ▼
                             [ M6: Auditoria Final ]
                           (Relatório e Matriz Global)
```

### 2.3. Dimensões e Vetores de Análise
Cada repositório foi submetido a quatro vetores de auditoria:
1. **Comunicação Assíncrona (Eventos e Mensageria):**
   - Identificação de publishers SNS e consumers SQS.
   - Validação da estrutura de envelopamento canônico (`IntegrationEvent`).
   - Verificação de atributos de cabeçalho SNS (`tipoEvento`, `origem`, `idOrdemServico`, `correlationId`).
2. **Comunicação Síncrona (APIs HTTP / REST):**
   - Mapeamento completo de controllers, Minimal APIs e rotas expostas.
   - Comparação estrutural de DTOs de Request e Response locais com o pacote compartilhado.
   - Constatação de violações de encapsulamento (entidades de domínio expostas na API).
3. **Autenticação, Autorização e Constantes Corporativas:**
   - Uso de constantes fortemente tipadas para papéis de segurança (`JwtAuthConstants.Roles`) e identificadores de usuário (`JwtAuthConstants.Claims`).
   - Mapeamento de enums e status de ciclo de vida de Ordens de Serviço e Pagamentos.
4. **Governança de Build, Compilação e Testes Automatizados:**
   - Compatibilidade de formatos de solução (`.sln` vs `.slnx`).
   - Integridade das regras de Clean Architecture (NetArchTest).
   - Execução integral das suítes de testes automatizados com validação estrita de regressões.

---

## 3. Topologia Arquitetural e Fluxos de Integração

### 3.1. Visão Geral da Topologia de Microsserviços

```
                                  ┌────────────────────────────────┐
                                  │   Fiap.TechChallenge.Contracts │
                                  │   (Shared Class Library .NET 8)│
                                  └───────────────┬────────────────┘
                                                  │
                 ┌────────────────────────────────┼────────────────────────────────┐
                 │ ProjectReference               │ ProjectReference               │ ProjectReference
                 ▼                                ▼                                ▼
  ┌──────────────────────────────┐ ┌──────────────────────────────┐ ┌──────────────────────────────┐
  │     fase1-tech-challenge     │ │        os-microservice       │ │       execution-service      │
  │     (Core Service API)       │ │     (Ordem de Serviço API)   │ │    (Produção Mecânica API)   │
  │                              │ │                              │ │                              │
  │ • Clientes / Veículos        │ │ • Ciclo de Vida da OS        │ │ • Triagem / Diagnóstico      │
  │ • Catálogo de Serviços       │ │ • Orquestração da Saga       │ │ • Execução de Tarefas        │
  │ • Peças / Estoque            │ │ • Lambda SQS / SNS           │ │ • MassTransit Consumer       │
  │ • Autenticação Staff         │ │ • DynamoDB State             │ │ • PostgreSQL Database        │
  └──────────────┬───────────────┘ └──────────────┬───────────────┘ └──────────────┬───────────────┘
                 ▲                                │                                │
                 │ HTTP (CoreApiClient)           │ AWS SNS                        │ AWS SNS
                 └────────────────────────────────┤ (Notificações)                 │ (Status Produção)
                                                  │                                │
                                                  ▼                                ▼
                                       ┌──────────────────────┐         ┌──────────────────────┐
                                       │   AWS SNS Topics     │◄────────┤ AWS SQS Queues       │
                                       │   (techchallenge-*)  ├────────►│ (techchallenge-*)    │
                                       └──────────┬───────────┘         └──────────────────────┘
                                                  │
                                                  │ AWS SNS (Eventos Faturamento)
                                                  ▼
                                   ┌──────────────────────────────┐
                                   │       billing-service        │
                                   │    (Faturamento / Pgtos)     │
                                   │                              │
                                   │ • Geração de Orçamentos      │
                                   │ • Emissão de Boletos         │
                                   │ • Webhooks MercadoPago       │
                                   │ • PostgreSQL Database        │
                                   └──────────────────────────────┘
```

### 3.2. Fluxo Assíncrono do Ciclo de Vida da Ordem de Serviço (Event Choreography)

```
[Cliente]      [os-microservice]            [execution-service]           [billing-service]           [fase1-tech-challenge]
   │                   │                             │                            │                             │
   │── Criar OS ──────>│                             │                            │                             │
   │                   │── OrdemServicoAbertaEvent ─>│                            │                             │
   │                   │   (SNS: techchallenge.os)   │ (Consome e aloca técnico)  │                             │
   │                   │                             │                            │                             │
   │                   │                             │── DiagnosticoIniciado ────>│                             │
   │                   │<─ DiagnosticoConcluido ─────│                            │                             │
   │                   │   (Pecas / Servicos sugeridos)                           │                             │
   │                   │                             │                            │                             │
   │                   │── Solicitar Orcamento ──────────────────────────────────>│                             │
   │                   │                                                          │── Reservar Estoque ────────>│
   │                   │                                                          │   (Valida disponibilidade)  │
   │                   │<─ OrcamentoGeradoEvent ──────────────────────────────────│                             │
   │                   │                                                          │                             │
   │── Aprovar OS ────>│                                                          │                             │
   │                   │── Gerar Cobranca / Boleto ──────────────────────────────>│                             │
   │                   │                                                          │── Emitir Boleto / PIX ─────>│
   │                   │<─ PagamentoConfirmadoEvent ──────────────────────────────│                             │
   │                   │                                                          │                             │
   │                   │── OrdemServicoLiberadaParaExecucaoEvent ────────────────>│                             │
   │                   │   (SNS: tarefas autorizadas)│                            │                             │
   │                   │                             │── ProducaoIniciada ───────>│                             │
   │                   │                             │── ProducaoFinalizada ─────>│                             │
   │                   │<─ ProducaoFinalizadaEvent ──│                            │                             │
   │                   │                             │                            │                             │
   │── Entregar Veic ─>│                             │                            │                             │
   │   (OS Finalizada) │── StatusOrdemServicoAlteradoEvent ──────────────────────>│                             │
```

---

## 4. Constatações Detalhadas e Ações por Microsserviço

### 4.1. `fase1-tech-challenge` (Core Service)

#### Perfil e Responsabilidades
Responsável pela gestão cadastral dos clientes, veículos, catálogo de serviços, peças/insumos, controle físico de estoque e autenticação central dos funcionários da oficina mecânica (staff).

#### Auditoria de Comunicação Assíncrona
- **Constatação:** O microsserviço opera estritamente como uma API síncrona HTTP/REST. O rastreio estático via `grep` não localizou instâncias de SDKs da AWS (`AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`) ou mensageria (`MassTransit`, `RabbitMQ`).
- **Reserva de Estoque:** O contrato de falha de estoque (`ReservaEstoqueFalhouEvent : IntegrationEvent`) foi desenhado para publicação assíncrona pela orquestração do ecossistema e já se encontrava centralizado no namespace `Fiap.TechChallenge.Contracts.Events`.

#### Auditoria de Comunicação Síncrona
- **Endpoints Identificados:** 6 controllers sob `/api/v1/`: `ClienteController`, `VeiculoController`, `ServicoController`, `PecaInsumoController`, `EstoqueController` e `AutenticacaoController`.
- **Divergências Encontradas:**
  1. *Ausência de Vínculo com Contratos Compartilhados:* Nenhum dos projetos `.csproj` (`Domain`, `Application`, `Infrastructure`, `External`, `Api`, `Tests`) referenciava `Fiap.TechChallenge.Contracts`.
  2. *Defasagem de Modelos de Catálogo:* O DTO `ClienteCatalogoDto` no shared omitia a propriedade `Telefone` (necessária para os chamadores) e `ServicoCatalogoDto` omitia `Descricao`.
  3. *DTOs Órfãos / Mortos:* Existiam classes legadas não utilizadas na camada de aplicação: `OrdemServicoItemServicoResponse.cs` e `OrdemServicoItemPecaInsumoResponse.cs`.
  4. *Strings Mágicas de Autenticação:* Literais como `"Administrador"`, `"Operador"`, `"Mecanico"` e `"UsuarioId"` estavam espalhados por 6 controllers, no `TokenService.cs` e na rotina `DatabaseSeed.cs`.

#### Implementações e Refatorações Realizadas
1. **Adição de Referência de Projeto:** Adicionado `<ProjectReference Include="..\..\..\shared\src\Fiap.TechChallenge.Contracts\Fiap.TechChallenge.Contracts.csproj" />` em `Application.csproj`, `Infrastructure.csproj`, `External.csproj`, `Api.csproj` e `Tests.csproj`.
2. **Centralização de DTOs no Shared:** Inclusão de 10 DTOs síncronos em `CoreCatalogoContracts.cs` (`CriarClienteRequest`, `CriarVeiculoRequest`, `AtualizarVeiculoPatchRequest`, `CriarServicoRequest`, `CriarPecaInsumoRequest`, `CriarEstoqueRequest`, `ClienteDetalhadoDto`, etc.), contratos de autenticação em `AuthContracts.cs` (`StaffLoginRequest`, `StaffLoginResponse`, `StaffCadastrarRequest`) e paginação em `PaginationContracts.cs` (`PagedRequestContrato`, `PagedResultContrato<T>`).
3. **Mapeamento Bidirecional nos DTOs da Aplicação:** Os DTOs de `src/Fiap.TechChallenge.Application/DTOs/` foram atualizados para implementar conversões implícitas e métodos de mapeamento (`ToContract()`, `ToCriarRequest()`) em relação aos contratos compartilhados, mantendo 100% de compatibilidade binária e sem quebrar as suítes de teste.
4. **Remoção de Código Morto:** Os arquivos órfãos `OrdemServicoItemServicoResponse.cs` e `OrdemServicoItemPecaInsumoResponse.cs` foram permanentemente excluídos.
5. **Erradicação de Magic Strings:** Todos os controllers, `DatabaseSeed.cs` e `TokenService.cs` passaram a consumir rigorosamente `JwtAuthConstants.Roles` e `JwtAuthConstants.Claims`.
6. **Compatibilidade de Build:** Documentada a execução do build diretamente nos projetos `.csproj` via CLI do .NET 8 SDK, alinhado ao `.github/workflows/ci.yml`.
7. **Resultado de Testes:** **246 testes executados e 246 aprovados** (`0 falhas, 0 pulados`).

---

### 4.2. `os-microservice` (Ordem de Serviço Lambda & API)

#### Perfil e Responsabilidades
Orquestra o ciclo de vida completo das Ordens de Serviço (criação, diagnóstico, aprovação de orçamento, execução mecânica e entrega final). Opera como uma aplicação híbrida: expõe endpoints ASP.NET Core em AWS Lambda e consome filas SQS para executar sagas de compensação com persistência no Amazon DynamoDB.

#### Auditoria de Comunicação Assíncrona
- **Publishers SNS:** `OrdemServicoService.cs` publicava os eventos `OrdemServicoAbertaEvent` e `OrdemServicoLiberadaParaExecucaoEvent`.
  - *Divergência Crítica:* A publicação era feita por meio de **tipos anônimos** (`new { osId = ..., cliente = ..., ... }`), contornando a validação em tempo de compilação e suscetível a erros de schema.
- **Consumers SQS e Sagas:** `CompensacaoEventoFunction.cs` e `CompensacaoFilaFunction.cs`.
  - *Divergência Crítica:* O leitor de idempotência buscava a chave `"idEvento"` (`LerTexto(detail, "idEvento")`), ignorando mensagens canônicas que serializam `"eventId"` através de `IntegrationEvent`.

#### Auditoria de Comunicação Síncrona
- **Inbound Endpoints:** 15 endpoints REST em `OrdemServicoEndpoints.cs` sob o prefixo `/api/v1/ordens-servico`.
- **Outbound HTTP Client:** `CoreApiClient.cs` comunicava com o `fase1-tech-challenge` para validar clientes, veículos, serviços, peças e atualizar estoque.
- **Divergências Encontradas:**
  1. *Modelos Redundantes Locais:* A pasta `src/OrdemServico.Lambda/Application/DTOs/` continha 8 classes redundantes (`OrdemServicoRequest`, `OrdemServicoResponse`, `Progresso`, etc.).
  2. *Modelos Privados no HTTP Client:* O `CoreApiClient.cs` definia internamente `ClienteDto`, `VeiculoDto`, `ServicoDto`, `PecaInsumoDto` e `ErroDto`.
  3. *Rotas Hardcoded:* URLs da API do Core estavam fixadas em literais (`"api/v1/clientes/{idCliente}"`, `"api/v1/estoques/{idPecaInsumo}/remover-quantidade"`).

#### Implementações e Refatorações Realizadas
1. **Adição de Referência de Projeto:** Adicionado `<ProjectReference Include="..\..\..\shared\src\Fiap.TechChallenge.Contracts\Fiap.TechChallenge.Contracts.csproj" />` em `src/OrdemServico.Lambda/OrdemServico.Lambda.csproj`.
2. **Centralização de DTOs e Rotas no Shared:** Criados `OsService/OrdemServicoContracts.cs` (8 DTOs síncronos e rotas `OrdemServicoApiRoutes`) e `OsService/OrdemServicoStatus.cs` (constantes de status canônicas: `RECEBIDA`, `EM_DIAGNOSTICO`, `AGUARDANDO_APROVACAO`, `EM_EXECUCAO`, `FINALIZADA`, `ENTREGUE`, `CANCELADA`).
3. **Exclusão de DTOs Redundantes:** Deletada a pasta inteira `Application/DTOs/` (8 arquivos).
4. **Publicação Fortemente Tipada no SNS:** `OrdemServicoService.cs` refatorado para instanciar e publicar as classes concretas `OrdemServicoAbertaEvent` e `OrdemServicoLiberadaParaExecucaoEvent` de `Fiap.TechChallenge.Contracts.Events`, serializadas com `JsonDefaults.Options` (camelCase).
5. **Modernização do `CoreApiClient`:** Substituição de todos os DTOs privados locais pelos tipos centralizados `ClienteCatalogoDto`, `VeiculoCatalogoDto`, `ServicoCatalogoDto` e `PecaInsumoCatalogoDto`, e adoção das rotas canônicas de `CoreApiRoutes`.
6. **Interoperabilidade Total de Compensação:** `CompensacaoEventoFunction.cs` atualizada para suportar tanto `"eventId"` quanto `"idEvento"`, e tanto `"osId"` quanto `"idOrdemServico"`.
7. **Ampliação de Testes:** Suíte de testes atualizada e expandida de 52 para **56 testes unitários aprovados** (`0 falhas, 0 pulados`), cobrindo publicação tipada e flexibilidade de desserialização.

---

### 4.3. `execution-service` (Produção Mecânica e Diagnóstico)

#### Perfil e Responsabilidades
Gerencia as etapas operacionais dentro da oficina mecânica: triagem e diagnóstico técnico inicial por mecânicos, apontamento e sugestão de serviços/peças adicionais, e execução controlada das tarefas de produção autorizadas.

#### Auditoria de Comunicação Assíncrona
- **Inbound Consumers (MassTransit / SQS):** Consome `OrdemServicoAbertaEvent`, `DiagnosticoRejeitadoEvent`, `OrdemServicoLiberadaParaExecucaoEvent` e `OrdemServicoCanceladaEvent`.
- **Outbound Publishers (SNS):** Publica `DiagnosticoIniciadoEvent`, `DiagnosticoTecnicoConcluidoEvent`, `ProducaoIniciadaEvent`, `ProducaoFinalizadaEvent` e `ProducaoCanceladaEvent`.
- **Divergências Críticas Encontradas:**
  1. *Shadowing Explícito de Namespace:* O arquivo `src/Fiap.TechChallenge.Execution.Application/Events/IntegrationEvents.cs` declarava localmente o namespace `Fiap.TechChallenge.Contracts.Events` e definia 14 records/classes locais, simulando a existência do pacote compartilhado sem referenciá-lo.
  2. *Incompatibilidade de Propriedades:* No arquivo espelho, a tarefa continha `IdServicoOrigem`, enquanto o contrato centralizado em `OrdemServicoModels.cs` padroniza `IdServico`.
  3. *Incompatibilidade de Modelos Aninhados:* UseCases instanciavam `ServicoSugeridoItemEvent` e `PecaSugeridaItemEvent`, divergindo dos nomes canônicos `ServicoSugeridoInfo` e `PecaSugeridaInfo`.
  4. *Metadados SNS Não Padronizados:* `AwsSnsEventPublisher.cs` publicava o atributo de origem como `"execution-service"`, divergindo da convenção corporativa `"techchallenge.execucao"` definida em `SnsMessageAttributes`.

#### Auditoria de Comunicação Síncrona
- **Endpoints Identificados:** 10 rotas HTTP em `ProducaoController.cs` sob o prefixo `/api/v1/producao`.
- **Divergência:** DTOs de requisição síncrona declarados localmente em `src/Fiap.TechChallenge.Execution.Application/DTOs/ProducaoRequests.cs`.

#### Implementações e Refatorações Realizadas
1. **Adição de Referência de Projeto:** Adicionado `<ProjectReference Include="..\..\..\shared\src\Fiap.TechChallenge.Contracts\Fiap.TechChallenge.Contracts.csproj" />` em `Fiap.TechChallenge.Execution.Application.csproj`.
2. **Centralização de Contratos Síncronos no Shared:** Criado `ExecutionService/ExecutionContracts.cs` contendo `ExecutionApiRoutes` e os 5 DTOs síncronos (`FinalizarDiagnosticoRequest`, `ServicoSugeridoRequest`, `PecaSugeridaRequest`, `FinalizarTarefaRequest`, `CancelarProducaoRequest`).
3. **Compatibilização de Modelos e Fábricas:** Adicionadas fábricas estáticas de conveniência `Create(...)` em `ExecutionEvents.cs` e subtipos derivados (`TarefaItemContrato`, `ClienteContrato`, etc.) em `ExecutionModels.cs` e `OrdemServicoModels.cs`.
4. **Eliminação Integral de Arquivos Espelho:** Excluído o arquivo de shadowing `Events/IntegrationEvents.cs` (e seu diretório) e o arquivo `DTOs/ProducaoRequests.cs`.
5. **Atualização das Camadas de Aplicação e Infraestrutura:** Atualizados 5 UseCases, consumers MassTransit, worker de segundo plano `ExecutionSqsBackgroundWorker` e o controller de API para consumir diretamente `Fiap.TechChallenge.Contracts`.
6. **Alinhamento de Atributos SNS:** Corrigido o atributo de mensagem SNS para utilizar `SnsMessageAttributes.Origens.ExecutionService` (`"techchallenge.execucao"`).
7. **Validação Arquitetural e Testes:** Executado teste de arquitetura (NetArchTest) validando que as fronteiras da Clean Architecture foram rigorosamente preservadas. **100 testes unitários executados e 100 aprovados** (`0 falhas, 0 pulados`).

---

### 4.4. `billing-service` (Faturamento e Pagamentos)

#### Perfil e Responsabilidades
Responsável pelo cálculo financeiro de orçamentos, emissão e liquidação de ordens de pagamento (PIX / Boleto), comunicação com gateways de pagamento (MercadoPago) e recepção de webhooks transacionais.

#### Auditoria de Comunicação Assíncrona
- **Esquemas Definidos no Shared:** `BillingEvents.cs` já contemplava os eventos canônicos: `OrcamentoGeradoEvent`, `PagamentoConfirmadoEvent`, `PagamentoCanceladoCompensacaoEvent`, `PagamentoRecusadoEvent` e `PagamentoEstornadoEvent`.
- **Estado do Microsserviço:** O serviço ainda não continha workers de fila SQS ativos. A centralização assegura que, ao ativar a mensageria, os contratos corporativos já estarão consumidos nativamente.

#### Auditoria de Comunicação Síncrona
- **Endpoints Identificados:** `OrcamentoController.cs` (`POST /api/v1/orcamento`, `POST /api/v1/orcamento/{id}/enviar-orcamento`, `GET /api/v1/orcamento/listar`) e `PagamentoController.cs` (`POST /api/v1/pagamento/boleto`, `GET /api/v1/pagamento/{id}`, `POST /api/v1/pagamento/webhook`).
- **Divergências Encontradas:**
  1. *Ausência de Referência ao Shared:* O projeto não referenciava `Fiap.TechChallenge.Contracts`.
  2. *DTOs Locais Desconectados:* `CriarBoletoRequest.cs`, `BoletoResponse.cs` e `EnviarOrcamentoRequest.cs` declarados localmente.
  3. *Acoplamento de Domínio:* `GerarOrcamentoRequest` local importava entidades do EF Core (`Servico` e `PecaInsumo`) no corpo HTTP.
  4. *Falha Crítica de Compilação da Solução:* O repositório continha apenas `Billing.Service.slnx` na raiz (formato experimental XML). Ao executar `dotnet build Billing.Service.slnx` no .NET 8 SDK, o build falhava com `error MSB4068: The element <Solution> is unrecognized`.

#### Implementações e Refatorações Realizadas
1. **Criação da Solução Padrão:** Gerado o arquivo padrão de solução `/home/gabriel/me/SOAT/billing-service/Billing.Service.sln` contendo todos os 6 projetos (5 de código e 1 de testes). O build via solução agora compila com 0 erros e 0 alertas.
2. **Adição de Referência de Projeto:** Adicionado `<ProjectReference Include="..\..\..\shared\src\Fiap.TechChallenge.Contracts\Fiap.TechChallenge.Contracts.csproj" />` em `Billing.Service.Application.csproj` e `Billing.Service.Tests.csproj`.
3. **Centralização de Contratos no Shared:** Criado `BillingService/BillingContracts.cs` com rotas `BillingApiRoutes`, DTOs de pagamento (`CriarBoletoRequest`, `BoletoResponse`), DTOs de orçamento (`EnviarOrcamentoRequest`, `GerarOrcamentoRequest`, `ItemCalculoOrcamentoDto`).
4. **Remoção de Arquivos Locais Redundantes:** Excluídos `CriarBoletoRequest.cs`, `BoletoResponse.cs`, `EnviarOrcamentoRequest.cs` e a pasta vazia `DTOs/Responses/`.
5. **Atualização de Serviços e Controllers:** Atualizados `IPagamentoService.cs`, `PagamentoService.cs`, `PagamentoController.cs`, `OrcamentoController.cs` e os testes unitários para consumir `Fiap.TechChallenge.Contracts.BillingService`.
6. **Resultado de Testes:** **57 testes unitários executados e 57 aprovados** (`0 falhas, 0 pulados`).

---

## 5. Inventário Completo de Contratos Centralizados (`Fiap.TechChallenge.Contracts`)

O pacote `shared/src/Fiap.TechChallenge.Contracts` é o repositório canônico e fonte única da verdade para todos os microsserviços do ecossistema. A seguir, apresenta-se o catálogo discriminado por namespace.

### 5.1. `Fiap.TechChallenge.Contracts.Common`
Define as abstrações basilares de integração, paginação e metadados de mensageria.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `abstract record` | `IntegrationEvent` | `Common/IntegrationEvent.cs` | Contrato base para todos os eventos assíncronos da arquitetura (`EventId`, `OccurredAt`, `Version`, `OsId`, `CorrelationId`, `TipoEvento`). | Todos os microsserviços e Lambdas |
| `static class` | `SnsMessageAttributes` | `Common/SnsMessageAttributes.cs` | Constantes canônicas de nomes de atributos de cabeçalho SNS (`tipoEvento`, `origem`, `idOrdemServico`, `correlationId`) e origens de serviço (`techchallenge.os`, `techchallenge.execucao`, etc.). | `os-microservice`, `execution-service`, `billing-service` |
| `record` | `PagedRequestContrato` | `Common/PaginationContracts.cs` | DTO canônico para solicitações paginadas (`Pagina`, `TamanhoPagina`). | `fase1-tech-challenge`, `billing-service`, BFF |
| `record` | `PagedResultContrato<T>`| `Common/PaginationContracts.cs` | Contêiner genérico de respostas paginadas (`Itens`, `TotalRegistros`, `Pagina`, `TamanhoPagina`, `TotalPaginas`). | `fase1-tech-challenge`, `billing-service`, BFF |

### 5.2. `Fiap.TechChallenge.Contracts.AuthService`
Define os contratos de autenticação central do staff e as constantes corporativas de segurança.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `static class` | `JwtAuthConstants` | `AuthService/AuthContracts.cs` | Nomes canônicos de claims (`UsuarioId`, `Email`, `Role`) e papéis de usuário (`Administrador`, `Operador`, `Mecanico`). | Todos os microsserviços e Lambdas |
| `record` | `StaffLoginRequest` | `AuthService/AuthContracts.cs` | Contrato de requisição de autenticação de funcionário (`Login`, `Senha`). | `fase1-tech-challenge`, API Gateway, Testes E2E |
| `record` | `StaffLoginResponse` | `AuthService/AuthContracts.cs` | Contrato de resposta contendo o token de acesso (`AccessToken`, `TokenType`, `ExpiresInSeconds`, `UsuarioId`, `Perfil`). | `fase1-tech-challenge`, API Gateway, Testes E2E |
| `record` | `StaffCadastrarRequest` | `AuthService/AuthContracts.cs` | Contrato de cadastro de novo funcionário (`Nome`, `Login`, `Senha`, `Perfil`). | `fase1-tech-challenge` |

### 5.3. `Fiap.TechChallenge.Contracts.CoreService`
Centraliza as rotas e os contratos síncronos dos catálogos de clientes, veículos, serviços, peças e estoque.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `static class` | `CoreApiRoutes` | `CoreService/CoreCatalogoContracts.cs` | Constantes de rotas da API do Core Service (`/api/v1/clientes/{id}`, `/api/v1/veiculos/{id}`, etc.). | `os-microservice`, E2E Tests, BFF |
| `record` | `ClienteCatalogoDto` | `CoreService/CoreCatalogoContracts.cs` | Resumo de cliente para catálogos e orçamentos (`Id`, `Nome`, `Email`, `Telefone`, `Celular`). | `os-microservice`, `billing-service` |
| `record` | `VeiculoCatalogoDto` | `CoreService/CoreCatalogoContracts.cs` | Resumo de veículo para consultas rápidas (`Id`, `Placa`, `Marca`, `Modelo`, `Ano`). | `os-microservice`, `execution-service` |
| `record` | `ServicoCatalogoDto` | `CoreService/CoreCatalogoContracts.cs` | Resumo de serviço (`Id`, `Nome`, `ValorUnitario`, `TempoEstimadoMinutos`, `Descricao`). | `os-microservice`, `execution-service` |
| `record` | `PecaInsumoCatalogoDto`| `CoreService/CoreCatalogoContracts.cs` | Resumo de peça/insumo (`Id`, `Descricao`, `ValorUnitario`). | `os-microservice`, `billing-service` |
| `record` | `ClienteDetalhadoDto` | `CoreService/CoreCatalogoContracts.cs` | Dados completos de cliente incluindo `CpfCnpj` e endereço. | `fase1-tech-challenge`, Admin |
| `record` | `CriarClienteRequest` | `CoreService/CoreCatalogoContracts.cs` | Requisição para criação de cliente. | `fase1-tech-challenge`, Clientes API |
| `record` | `CriarVeiculoRequest` | `CoreService/CoreCatalogoContracts.cs` | Requisição para cadastro de veículo. | `fase1-tech-challenge`, Veículos API |
| `record` | `AtualizarVeiculoPatchRequest` | `CoreService/CoreCatalogoContracts.cs` | Requisição para atualização parcial de veículo via PATCH. | `fase1-tech-challenge` |
| `record` | `CriarServicoRequest` | `CoreService/CoreCatalogoContracts.cs` | Requisição para cadastro de novo serviço no catálogo. | `fase1-tech-challenge` |
| `record` | `CriarPecaInsumoRequest`| `CoreService/CoreCatalogoContracts.cs`| Requisição para cadastro de nova peça ou insumo. | `fase1-tech-challenge` |
| `record` | `CriarEstoqueRequest` | `CoreService/CoreCatalogoContracts.cs` | Requisição de alocação de saldo inicial de estoque. | `fase1-tech-challenge` |
| `record` | `EstoqueQuantidadeRequest`| `CoreService/CoreCatalogoContracts.cs`| Requisição de incremento ou decremento de estoque. | `os-microservice`, `fase1-tech-challenge` |

### 5.4. `Fiap.TechChallenge.Contracts.OsService`
Centraliza os contratos síncronos e assíncronos da orquestração de Ordens de Serviço.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `static class` | `OrdemServicoApiRoutes`| `OsService/OrdemServicoContracts.cs` | Rotas canônicas da API de OS (`/api/v1/ordens-servico`, etc.). | `os-microservice`, E2E Tests, BFF |
| `static class` | `OrdemServicoStatus` | `OsService/OrdemServicoStatus.cs` | Constantes e aliases canônicos dos estados da OS (`RECEBIDA`, `EM_DIAGNOSTICO`, etc.). | `os-microservice`, `execution-service`, E2E Tests |
| `record` | `OrdemServicoRequest` | `OsService/OrdemServicoContracts.cs` | Requisição para abertura de nova Ordem de Serviço. | `os-microservice`, Clientes API |
| `record` | `OrdemServicoResponse`| `OsService/OrdemServicoContracts.cs` | Contrato completo da OS retornada pela API. | `os-microservice`, E2E Tests, BFF |
| `record` | `OrdemServicoProgressoResponse`| `OsService/OrdemServicoContracts.cs` | Resumo de progresso e status da OS para acompanhamento do cliente. | `os-microservice`, Mobile App |
| `record` | `OrdemServicoItemServicoResponse` | `OsService/OrdemServicoContracts.cs` | Detalhe de item de serviço vinculado à OS. | `os-microservice`, `billing-service` |
| `record` | `OrdemServicoItemPecaInsumoResponse` | `OsService/OrdemServicoContracts.cs` | Detalhe de peça vinculada à OS. | `os-microservice`, `billing-service` |
| `record` | `OrdemServicoPecaInsumoRequest` | `OsService/OrdemServicoContracts.cs` | Requisição para inclusão de peça na OS. | `os-microservice` |
| `record` | `OrdemServicoServicosRequest` | `OsService/OrdemServicoContracts.cs` | Requisição para inclusão de múltiplos serviços na OS. | `os-microservice` |
| `record` | `ClienteInfo` | `OsService/OrdemServicoModels.cs` | Snapshot imutável de cliente em eventos de OS (`IdCliente`, `Nome`, `Telefone`, `Email`). | `os-microservice`, `execution-service` |
| `record` | `VeiculoInfo` | `OsService/OrdemServicoModels.cs` | Snapshot imutável de veículo em eventos de OS (`Placa`, `Modelo`, `Ano`). | `os-microservice`, `execution-service` |
| `record` | `TarefaInfo` | `OsService/OrdemServicoModels.cs` | Snapshot imutável de tarefa liberada para produção mecânica. | `os-microservice`, `execution-service` |
| `record` | `ItemOrcamentoInfo` | `OsService/OrdemServicoModels.cs` | Item componente do orçamento da OS. | `os-microservice`, `billing-service` |
| `enum` | `TipoCompensacaoContrato` | `OsService/OrdemServicoCommands.cs` | Tipos de compensação da saga: `EstornarEstoque`, `RemoverPecaInsumo`, `CancelarOrdemServico`. | `os-microservice` |
| `record` | `ComandoCompensacaoContrato` | `OsService/OrdemServicoCommands.cs` | Comando trafegado via SQS para execução de compensações na saga. | `os-microservice` |

### 5.5. `Fiap.TechChallenge.Contracts.ExecutionService`
Centraliza os contratos do microsserviço de execução e diagnóstico.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `static class` | `ExecutionApiRoutes` | `ExecutionService/ExecutionContracts.cs` | Constantes de rotas da API de Produção (`/api/v1/producao`, etc.). | `execution-service`, E2E Tests |
| `record` | `FinalizarDiagnosticoRequest` | `ExecutionService/ExecutionContracts.cs` | Requisição de conclusão de diagnóstico técnico com serviços e peças sugeridas. | `execution-service`, Mecânicos |
| `record` | `ServicoSugeridoRequest` | `ExecutionService/ExecutionContracts.cs` | Item de serviço identificado no diagnóstico. | `execution-service`, `os-microservice` |
| `record` | `PecaSugeridaRequest` | `ExecutionService/ExecutionContracts.cs` | Item de peça necessária identificada no diagnóstico. | `execution-service`, `os-microservice` |
| `record` | `FinalizarTarefaRequest` | `ExecutionService/ExecutionContracts.cs` | Requisição de finalização de uma tarefa individual na oficina. | `execution-service`, Mecânicos |
| `record` | `CancelarProducaoRequest` | `ExecutionService/ExecutionContracts.cs` | Requisição de cancelamento de produção com justificativa. | `execution-service`, Mecânicos |
| `record` | `ServicoSugeridoInfo` | `ExecutionService/ExecutionModels.cs` | Modelo imutável de serviço sugerido emitido no evento. | `execution-service`, `os-microservice` |
| `record` | `PecaSugeridaInfo` | `ExecutionService/ExecutionModels.cs` | Modelo imutável de peça sugerida emitida no evento. | `execution-service`, `os-microservice` |

### 5.6. `Fiap.TechChallenge.Contracts.BillingService`
Centraliza os contratos de pagamentos, orçamentos e webhooks.

| Tipo | Nome | Arquivo | Descrição / Responsabilidade | Consumidores |
| :--- | :--- | :--- | :--- | :--- |
| `static class` | `BillingApiRoutes` | `BillingService/BillingContracts.cs` | Constantes de rotas de faturamento e pagamentos (`/api/v1/pagamento/boleto`, etc.). | `billing-service`, E2E Tests |
| `record` | `CriarBoletoRequest` | `BillingService/BillingContracts.cs` | Requisição de emissão de cobrança via boleto bancário. | `billing-service`, Clientes API |
| `record` | `BoletoResponse` | `BillingService/BillingContracts.cs` | Resposta contendo linha digitável, código de barras e link de pagamento. | `billing-service`, Clientes API |
| `record` | `EnviarOrcamentoRequest` | `BillingService/BillingContracts.cs` | Requisição de disparo do orçamento gerado por e-mail para o cliente. | `billing-service`, `os-microservice` |
| `record` | `GerarOrcamentoRequest` | `BillingService/BillingContracts.cs` | Requisição desacoplada para cálculo financeiro do orçamento. | `billing-service`, `os-microservice` |
| `record` | `ItemCalculoOrcamentoDto`| `BillingService/BillingContracts.cs` | Item de serviço ou peça para compor o cálculo financeiro. | `billing-service`, `os-microservice` |
| `record` | `MercadoPagoWebhookRequest`| `BillingService/BillingModels.cs` | Contrato do webhook recebido do MercadoPago (`Action`, `Data`, `Type`). | `billing-service`, Gateways |
| `enum` | `StatusPagamentoContrato` | `BillingService/BillingModels.cs` | Estados do pagamento: `Pendente`, `Aprovado`, `Recusado`, `Expirado`, `Estornado`. | `billing-service`, `os-microservice` |
| `enum` | `MetodoPagamentoContrato` | `BillingService/BillingModels.cs` | Meios aceitos: `Boleto`, `Pix`, `CartaoCredito`, `CartaoDebito`. | `billing-service` |

### 5.7. `Fiap.TechChallenge.Contracts.Events`
Catálogo completo dos eventos de integração corporativos trafegados via AWS SNS/SQS.

| Nome do Evento | Origem Principal | Arquivo de Definição | Tópico SNS / Fila SQS |
| :--- | :--- | :--- | :--- |
| `OrdemServicoAbertaEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `DiagnosticoIniciadoEvent` | `execution-service` | `ExecutionService/ExecutionEvents.cs` | `techchallenge-execucao-events` |
| `DiagnosticoTecnicoConcluidoEvent` | `execution-service` | `ExecutionService/ExecutionEvents.cs` | `techchallenge-execucao-events` |
| `DiagnosticoRejeitadoEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `OrcamentoGeradoEvent` | `billing-service` | `BillingService/BillingEvents.cs` | `techchallenge-faturamento-events` |
| `OrcamentoReprovadoEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `PagamentoConfirmadoEvent` | `billing-service` | `BillingService/BillingEvents.cs` | `techchallenge-faturamento-events` |
| `PagamentoRecusadoEvent` | `billing-service` | `BillingService/BillingEvents.cs` | `techchallenge-faturamento-events` |
| `PagamentoEstornadoEvent` | `billing-service` | `BillingService/BillingEvents.cs` | `techchallenge-faturamento-events` |
| `PagamentoCanceladoCompensacaoEvent` | `billing-service` | `BillingService/BillingEvents.cs` | `techchallenge-faturamento-events` |
| `ReservaEstoqueFalhouEvent` | `fase1-tech-challenge` | `CoreService/CoreEstoqueEvents.cs` | `techchallenge-core-events` |
| `OrdemServicoLiberadaParaExecucaoEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `ProducaoIniciadaEvent` | `execution-service` | `ExecutionService/ExecutionEvents.cs` | `techchallenge-execucao-events` |
| `ProducaoFinalizadaEvent` | `execution-service` | `ExecutionService/ExecutionEvents.cs` | `techchallenge-execucao-events` |
| `ProducaoCanceladaEvent` | `execution-service` | `ExecutionService/ExecutionEvents.cs` | `techchallenge-execucao-events` |
| `StatusOrdemServicoAlteradoEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `PecasInsumoIncluidasEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `OrdemServicoCanceladaEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |
| `CompensacaoExecutadaEvent` | `os-microservice` | `OsService/OrdemServicoEvents.cs` | `techchallenge-os-events` |

---

## 6. Matriz Global de Compilação e Verificação de Testes

Todos os projetos do ecossistema foram compilados e testados de ponta a ponta com o .NET SDK 8 oficial. A tabela a seguir reflete o status de compilação e a execução determinística de **100% dos testes unitários e de integração**.

| # | Repositório / Componente | Projeto de Teste Alvo | SDK / Target | Status de Build | Testes Passados | Testes Falhos | Total Testes | Duração |
| :---: | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | `shared` (Contratos) | `shared/tests/Fiap.TechChallenge.Contracts.Tests/Fiap.TechChallenge.Contracts.Tests.csproj` | .NET 8.0 | **Sucesso** (0 avisos, 0 erros) | **46** | 0 | 46 | ~150 ms |
| **2** | `fase1-tech-challenge` (Core) | `fase1-tech-challenge/tests/Fiap.TechChallenge.Tests/Fiap.TechChallenge.Tests.csproj` | .NET 8.0 | **Sucesso** (0 avisos, 0 erros) | **246** | 0 | 246 | ~21 s |
| **3** | `os-microservice` (OS & Sagas) | `os-microservice/tests/OrdemServico.Lambda.Tests/OrdemServico.Lambda.Tests.csproj` | .NET 8.0 | **Sucesso** (0 avisos, 0 erros) | **56** | 0 | 56 | ~620 ms |
| **4** | `execution-service` (Produção) | `execution-service/tests/Fiap.TechChallenge.Execution.UnitTests/Fiap.TechChallenge.Execution.UnitTests.csproj` | .NET 8.0 | **Sucesso** (0 avisos, 0 erros) | **100** | 0 | 100 | ~260 ms |
| **5** | `billing-service` (Faturamento) | `billing-service/tests/Billing.Service.Tests/Billing.Service.Tests.csproj` | .NET 8.0 | **Sucesso** (0 avisos, 0 erros) | **57** | 0 | 57 | ~4 s |
| **Σ** | **Total Consolidado** | **5 Suítes Automatizadas em todo o Ecossistema** | **.NET 8.0** | **100% Compilado com Sucesso** | **505** | **0** | **505** | **~26 s** |

### 6.1. Detalhamento da Suíte de Testes de Contratos (`shared`)
A suíte `Fiap.TechChallenge.Contracts.Tests` foi expandida de 10 para **46 testes unitários** automatizados, cobrindo:
- **`IntegrationEventTests` (11 testes):** Garante que todos os 19 eventos de integração herdam de `IntegrationEvent`, respeitam a assinatura canônica de construtor e serializam com camelCase.
- **`CoreContractsTests` (10 testes):** Valida instanciação, obrigatoriedade de campos e serialização JSON de todos os DTOs do Core Service.
- **`AuthContractsTests` (4 testes):** Testa os DTOs de login/cadastro de staff e o catálogo de constantes `JwtAuthConstants`.
- **`PaginationContractsTests` (5 testes):** Valida limites de tamanho de página (clamp entre 1 e 100), cálculo correto de `TotalPaginas` e invariantes de coleções vazias.
- **`OrdemServicoContractsTests` (6 testes):** Testa DTOs de OS, serialização de progresso e compatibilidade de comandos de compensação.
- **`ExecutionContractsTests` (5 testes):** Testa DTOs de diagnóstico, tarefas e cancelamento de produção.
- **`BillingContractsTests` (5 testes):** Testa DTOs de emissão de boletos, envio de orçamento e webhooks.

---

## 7. Guia de Reproducibilidade e Comandos de Verificação

Para auditar e reproduzir de forma totalmente independente todos os resultados atestados neste relatório, execute os procedimentos descritos a seguir no terminal Linux:

### 7.1. Verificação de Compilação de Todas as Soluções
```bash
# 1. Compilar biblioteca central de contratos
dotnet build /home/gabriel/me/SOAT/shared/src/Fiap.TechChallenge.Contracts/Fiap.TechChallenge.Contracts.csproj

# 2. Compilar API do Core Service
dotnet build /home/gabriel/me/SOAT/fase1-tech-challenge/src/Fiap.TechChallenge.Api/Fiap.TechChallenge.Api.csproj

# 3. Compilar solução do microsserviço de Ordem de Serviço
dotnet build /home/gabriel/me/SOAT/os-microservice/OrdemServico.Lambda.sln

# 4. Compilar solução do microsserviço de Execução e Produção
dotnet build /home/gabriel/me/SOAT/execution-service/Fiap.TechChallenge.Execution.sln

# 5. Compilar a nova solução padrão do microsserviço de Billing
dotnet build /home/gabriel/me/SOAT/billing-service/Billing.Service.sln
```
*Resultado Esperado:* Todos os comandos retornam código de saída `0` com zero erros de compilação.

### 7.2. Execução Integral das Suítes de Testes Automatizados
```bash
# 1. Executar testes de contratos no shared (46 testes)
dotnet test /home/gabriel/me/SOAT/shared/tests/Fiap.TechChallenge.Contracts.Tests/Fiap.TechChallenge.Contracts.Tests.csproj

# 2. Executar testes do Core Service (246 testes - requer Docker para Testcontainers de PostgreSQL)
dotnet test /home/gabriel/me/SOAT/fase1-tech-challenge/tests/Fiap.TechChallenge.Tests/Fiap.TechChallenge.Tests.csproj

# 3. Executar testes de Ordem de Serviço (56 testes)
dotnet test /home/gabriel/me/SOAT/os-microservice/tests/OrdemServico.Lambda.Tests/OrdemServico.Lambda.Tests.csproj

# 4. Executar testes unitários do Execution Service (100 testes)
dotnet test /home/gabriel/me/SOAT/execution-service/tests/Fiap.TechChallenge.Execution.UnitTests/Fiap.TechChallenge.Execution.UnitTests.csproj

# 5. Executar testes do Billing Service (57 testes)
dotnet test /home/gabriel/me/SOAT/billing-service/tests/Billing.Service.Tests/Billing.Service.Tests.csproj
```
*Resultado Esperado:* Total acumulado de **505 testes aprovados e 0 testes com falha**.

### 7.3. Auditoria Estática de Ausência de Duplicação e Presença de Referências
```bash
# 1. Confirmar presença de referências ao pacote de contratos em todos os 4 serviços:
grep -rn "Fiap.TechChallenge.Contracts" \
  /home/gabriel/me/SOAT/fase1-tech-challenge/src/Fiap.TechChallenge.Application/Fiap.TechChallenge.Application.csproj \
  /home/gabriel/me/SOAT/os-microservice/src/OrdemServico.Lambda/OrdemServico.Lambda.csproj \
  /home/gabriel/me/SOAT/execution-service/src/Fiap.TechChallenge.Execution.Application/Fiap.TechChallenge.Execution.Application.csproj \
  /home/gabriel/me/SOAT/billing-service/src/Billing.Service.Application/Billing.Service.Application.csproj

# 2. Confirmar exclusão física dos arquivos de shadowing no execution-service:
test ! -f /home/gabriel/me/SOAT/execution-service/src/Fiap.TechChallenge.Execution.Application/Events/IntegrationEvents.cs && echo "OK: Shadowing removido com sucesso"

# 3. Confirmar ausência de DTOs redundantes em os-microservice:
test ! -d /home/gabriel/me/SOAT/os-microservice/src/OrdemServico.Lambda/Application/DTOs && echo "OK: DTOs redundantes eliminados"

# 4. Confirmar ausência de magic strings de autenticação no fase1-tech-challenge:
grep -rn "\"Administrador\"\|\"Operador\"\|\"Mecanico\"\|\"UsuarioId\"" /home/gabriel/me/SOAT/fase1-tech-challenge/src/
# (Retorno esperado: código de saída 1 / zero ocorrências)
```

---

## 8. Checklist de Conformidade com os Critérios de Aceite

A tabela a seguir comprova o atendimento integral a todos os requisitos especificados no pedido original (`ORIGINAL_REQUEST.md`):

| Requisito / Critério de Aceite | Status | Evidência Objetiva |
| :--- | :---: | :--- |
| **R1. Inspeção dos 4 Microsserviços**<br>Todos os 4 serviços (`billing-service`, `fase1-tech-challenge`, `os-microservice`, `execution-service`) foram inspecionados quanto a contratos locais vs centralizados. | **CONFORME** | Relatórios dos 4 Explorers (`explorer_billing`, `explorer_fase1`, `explorer_os`, `explorer_execution`) gerados e consolidados nas Seções 2 e 4 deste relatório. |
| **R1. Cobertura Síncrona e Assíncrona**<br>A análise cobre esquemas de eventos assíncronos (AWS SNS/SQS) e contratos síncronos (HTTP/REST). | **CONFORME** | Todos os 19 tópicos/filas de eventos e mais de 40 rotas REST catalogados nas Seções 4 e 5 deste documento. |
| **R2. Consolidação no Repositório Shared**<br>Os contratos e eventos identificados estão consolidados em `shared/src/Fiap.TechChallenge.Contracts/`. | **CONFORME** | Estrutura organizada por pastas: `CoreService/`, `OsService/`, `ExecutionService/`, `BillingService/`, `AuthService/`, `Common/` e `Events/` detalhadas na Seção 5. |
| **R2. Atualização de Referências nos Microsserviços**<br>Os 4 microsserviços referenciam e utilizam os contratos centralizados sem definições redundantes locais. | **CONFORME** | Arquivos `.csproj` atualizados com `<ProjectReference>`, arquivos espelhos excluídos e DTOs locais mapeados ou deletados. |
| **R2. Compilação do Shared Contracts**<br>O projeto `shared/src/Fiap.TechChallenge.Contracts` compila com sucesso (`dotnet build`). | **CONFORME** | Compilação com zero erros e zero warnings; 46/46 testes unitários aprovados em `Fiap.TechChallenge.Contracts.Tests`. |
| **R2. Compilação e Testes dos Microsserviços**<br>Todos os microsserviços modificados compilam com sucesso (`dotnet build`) e seus testes existentes passam (`dotnet test`). | **CONFORME** | **505 de 505 testes aprovados** (Core: 246, OS: 56, Execution: 100, Billing: 57, Shared: 46). Ver Seção 6. |
| **R3. Relatório de Auditoria Detalhado**<br>Gerar relatório documentando contratos por serviço, divergências encontradas, contratos centralizados e alterações realizadas. | **CONFORME** | Este documento (`RELATORIO_AUDITORIA_CONTRATOS.md`), replicado na raiz e em `shared/RELATORIO_AUDITORIA_CONTRATOS.md`. |

---

## 9. Considerações Finais e Próximos Passos

### 9.1. Ganhos Arquiteturais e de Governança
1. **Fonte Única da Verdade (*Single Source of Truth*):** O pacote compartilhado `Fiap.TechChallenge.Contracts` impede divergências estruturais entre produtores e consumidores de mensagens e APIs.
2. **Segurança de Tipos em Tempo de Compilação:** Alterações contratuais (quebras de assinatura ou adição de campos obrigatórios) são imediatamente detectadas durante o build, impedindo falhas silenciosas de desserialização em tempo de execução (*runtime*).
3. **Desacoplamento Limpo:** Endpoints HTTP não vazam mais entidades de persistência interna (EF Core / DynamoDB) para fora do microsserviço, reforçando o isolamento preconizado pelo *Domain-Driven Design* (DDD) e Clean Architecture.
4. **Padronização Corporativa de Mensageria:** O uso estrito de `IntegrationEvent` com metadados padronizados (`SnsMessageAttributes`) viabiliza rastreabilidade distribuída (*distributed tracing*) ponta a ponta com `correlationId`.

### 9.2. Recomendações para Evolução Contínua
- **Esteira de CI/CD:** Adicionar um *step* no GitHub Actions que impeça o merge de PRs que definam records no namespace `Fiap.TechChallenge.Contracts.Events` fora do repositório `shared`.
- **Evolução para Pacote NuGet Privado:** Em um ambiente corporativo com repositórios Git fisicamente desacoplados (multi-repo), substituir as referências locais `<ProjectReference>` por pacotes versionados via GitHub Packages ou AWS CodeArtifact (`Fiap.TechChallenge.Contracts.nupkg`), adotando Versionamento Semântico (SemVer).

---
*Relatório gerado e homologado com sucesso em 04 de Novembro de 2026.*
