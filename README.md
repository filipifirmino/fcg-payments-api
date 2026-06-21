# FCG Payments API — Grupo 14

Microsserviço responsável por simular o processamento de pagamentos da plataforma FIAP Cloud Games (FCG), operando exclusivamente via mensageria assíncrona (sem endpoints HTTP). Serviço criado na Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Domínio](#domínio)
- [Fluxo de Mensagens](#fluxo-de-mensagens)
- [Lógica de Simulação](#lógica-de-simulação)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Pré-requisitos](#pré-requisitos)
- [Rodando com Docker Compose](#rodando-com-docker-compose)
- [Rodando localmente](#rodando-localmente)
- [Kubernetes](#kubernetes)
- [Testes](#testes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Payments |
| Tipo | Worker Service (sem HTTP endpoints) |
| Banco de dados | PostgreSQL — `fcg_payments_db` |
| Consome | `OrderPlacedEvent` ← RabbitMQ (publicado pelo CatalogAPI) |
| Publica | `PaymentProcessedEvent` → RabbitMQ (consumido por CatalogAPI e NotificationsAPI) |

---

## Arquitetura

Serviço do tipo **Worker Service** — sem controllers ou endpoints HTTP. Toda comunicação ocorre via eventos RabbitMQ usando MassTransit.

O projeto segue **Clean Architecture** com separação estrita entre camadas:

```
Domain ← Application ← Infra ← Worker
```

### Padrões aplicados

- **Event-Driven Architecture** — reage exclusivamente a eventos; não expõe HTTP
- **Strategy Pattern** — `IApprovalStrategy` desacopla a lógica de simulação, tornando o serviço testável e extensível
- **Repository Pattern** — `IPaymentRepository` abstrai a persistência
- **Factory Method** — `Payment.Create()` encapsula as invariantes da entidade

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Tipo de projeto | Worker Service | — |
| ORM | Entity Framework Core + Npgsql | 10.0.9 |
| Banco de Dados | PostgreSQL | 16 |
| Mensageria | MassTransit + RabbitMQ | 8.1.3 |
| Logging | Serilog | 10.0.0 |
| Testes unitários | xUnit + Moq + FluentAssertions | — |
| Testes integração | Testcontainers + MassTransit TestHarness | — |
| Testes BDD | Reqnroll | 3.0.0 |

---

## Domínio

### Entidade Payment

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | Guid | Identificador único do pagamento |
| `OrderId` | Guid | Referência ao pedido originado no CatalogAPI |
| `UserId` | Guid | Referência ao usuário |
| `GameId` | Guid | Referência ao jogo adquirido |
| `GameTitle` | string | Título do jogo (desnormalizado) |
| `UserEmail` | string | E-mail do usuário (desnormalizado para notificações) |
| `Amount` | decimal | Valor processado |
| `Status` | PaymentStatus | `Approved` ou `Rejected` |
| `Reason` | string? | Motivo de rejeição (nulo quando aprovado) |
| `ProcessedAt` | DateTime | Data/hora UTC do processamento |

### Enum PaymentStatus

```csharp
public enum PaymentStatus { Approved, Rejected }
```

---

## Fluxo de Mensagens

```
CatalogAPI
    │
    │  OrderPlacedEvent  →  RabbitMQ
    │
    ▼
PaymentsAPI (OrderPlacedConsumer)
    │
    ├── chama PaymentService.ProcessAsync()
    ├── persiste Payment no PostgreSQL
    │
    │  PaymentProcessedEvent  →  RabbitMQ
    │
    ├──▶  CatalogAPI (PaymentProcessedConsumer)
    │         └── Status=Approved → adiciona jogo à biblioteca do usuário
    │
    └──▶  NotificationsAPI (PaymentProcessedConsumer)
              └── Status=Approved → loga e-mail de confirmação de compra
```

### OrderPlacedEvent (consumido)

```csharp
public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
    public string UserEmail { get; init; }
    public decimal Price { get; init; }
    public DateTime PlacedAt { get; init; }
}
```

### PaymentProcessedEvent (publicado)

```csharp
public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
    public string UserEmail { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
    public string? Reason { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

---

## Lógica de Simulação

O `PaymentService` simula aprovação/rejeição sem integração com gateway real. A decisão é delegada à `IApprovalStrategy`, permitindo troca sem alterar o serviço:

```
90% → Approved  (Reason: null)
10% → Rejected  (Reason: "Insufficient funds (simulated)")
```

A implementação padrão (`RandomApprovalStrategy`) usa `Random.Shared` — thread-safe e sem alocação por chamada. Em testes, a interface é mockada para resultados determinísticos.

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL | `Host=postgres;Port=5432;Database=fcg_payments_db;Username=fcg;Password=fcg_secret` |
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |
| `DOTNET_ENVIRONMENT` | Ambiente de execução | `Production` |

Copie `.env` e ajuste conforme necessário:

```bash
cp .env .env.local
```

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) (apenas para migrations locais)

```bash
dotnet tool install --global dotnet-ef
```

---

## Rodando com Docker Compose

Sobe PostgreSQL, RabbitMQ e o worker em conjunto:

```bash
docker compose up -d
```

Acompanhe os logs em tempo real:

```bash
docker compose logs -f payments-worker
```

Para derrubar:

```bash
docker compose down
```

> O worker aplica as migrations automaticamente ao iniciar — nenhuma ação manual necessária.

---

## Rodando localmente

### 1. Suba a infraestrutura

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
docker run -d --name postgres -e POSTGRES_USER=fcg -e POSTGRES_PASSWORD=fcg_secret -e POSTGRES_DB=fcg_payments_db -p 5432:5432 postgres:16-alpine
```

### 2. Restaure as dependências

```bash
dotnet restore
```

### 3. Execute o Worker

```bash
dotnet run --project src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj
```

### 4. (Opcional) Adicionar migrations manualmente

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/FCG-PAYMENTS-API.Infra/FCG-PAYMENTS-API.Infra.csproj \
  --startup-project src/FCG-PAYMENTS-API.Worker/FCG-PAYMENTS-API.Worker.csproj
```

---

## Kubernetes

Os manifests estão em `/k8s/`:

```bash
kubectl apply -f k8s/
```

| Arquivo | Conteúdo |
|---|---|
| `configmap.yaml` | RabbitMq__Host, RabbitMq__Username, DOTNET_ENVIRONMENT |
| `secret.yaml` | ConnectionStrings__Postgres, RabbitMq__Password |
| `deployment.yaml` | 1 replica, envFrom configmap + secret |
| `service.yaml` | Headless service (worker não expõe porta HTTP) |

Verificar pods:

```bash
kubectl get pods -l app=payments-worker
```

---

## Testes

### Executar todos os testes

```bash
dotnet test src/FCG-PAYMENTS-API.Tests/FCG-PAYMENTS-API.Tests.csproj
```

### Estratégia de testes

| Tipo | Classe | Cobertura |
|---|---|---|
| **Unit** | `PaymentServiceTests` | 15 testes — lógica de negócio, mapeamento, persistência |
| **Unit** | `PaymentRepositoryTests` | 11 testes — CRUD + GetByOrderId (InMemory EF) |
| **Integration** | `OrderPlacedConsumerIntegrationTests` | 10 testes — consumer end-to-end com Testcontainers PostgreSQL e MassTransit TestHarness |
| **BDD** | `PaymentService.feature` | 12 cenários — fluxos de aprovação, rejeição e mapeamento de dados |

Os testes de integração sobem um PostgreSQL real via Testcontainers e usam o MassTransit InMemory test harness — **sem dependência de RabbitMQ ou Docker externo**.

---

## Estrutura do Projeto

```
fcg-payments-api/
├── src/
│   ├── FCG-PAYMENTS-API.Domain/
│   │   ├── Common/              # Result<T>
│   │   ├── Entities/            # Payment
│   │   ├── Enums/               # PaymentStatus
│   │   └── Interfaces/          # IPaymentRepository, IPaymentService, IRepositoryBase
│   │
│   ├── FCG-PAYMENTS-API.Application/
│   │   ├── Configure/           # ApplicationConfigure (DI)
│   │   ├── Events/              # OrderPlacedEvent, PaymentProcessedEvent
│   │   ├── Interfaces/          # IApprovalStrategy
│   │   └── Services/            # PaymentService, RandomApprovalStrategy
│   │
│   ├── FCG-PAYMENTS-API.Infra/
│   │   ├── Configure/           # ConfigureInfra (DI + MassTransit)
│   │   ├── Consumers/           # OrderPlacedConsumer
│   │   ├── Migrations/          # EF Core migrations
│   │   ├── Repositories/        # RepositoryBase, PaymentRepository
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   │
│   ├── FCG-PAYMENTS-API.Worker/
│   │   ├── Program.cs           # Entry point, DI, auto-migrate
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── FCG-PAYMENTS-API.Tests/
│       ├── BDD/
│       │   ├── Features/        # PaymentService.feature
│       │   └── StepDefinitions/ # PaymentServiceSteps
│       ├── Integration/
│       │   ├── Config/          # WorkerTestFactory (Testcontainers)
│       │   └── Consumers/       # OrderPlacedConsumerIntegrationTests
│       └── Unit/
│           ├── Application/     # PaymentServiceTests
│           └── Infra/           # PaymentRepositoryTests
│
├── k8s/
│   ├── configmap.yaml
│   ├── secret.yaml
│   ├── deployment.yaml
│   └── service.yaml
│
├── Dockerfile
├── docker-compose.yml
├── .env
└── FCG-PAYMENTS-API.slnx
```

---

## Logs

Todos os eventos processados são registrados via **Serilog** com saída estruturada:

| Situação | Nível | Mensagem |
|---|---|---|
| Evento recebido | Information | `Received OrderPlacedEvent for OrderId {OrderId}` |
| Pagamento aprovado | Information | `Payment Approved for OrderId {OrderId}, Amount: {Amount}` |
| Pagamento rejeitado | Information | `Payment Rejected for OrderId {OrderId}, Amount: {Amount}` |

Em produção, os logs são gravados em `Logs/log-prod-{data}.txt` no formato JSON. Em desenvolvimento, saída no console com template legível.

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
