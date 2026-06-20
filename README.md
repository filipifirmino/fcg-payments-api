# FCG Payments API — Grupo 14

Microsserviço responsável por simular o processamento de pagamentos da plataforma FIAP Cloud Games (FCG), operando exclusivamente via mensageria assíncrona (sem endpoints HTTP). Serviço novo criado na Fase 2 — migração para microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Domínio](#domínio)
- [Fluxo de Mensagens](#fluxo-de-mensagens)
- [Lógica de Simulação](#lógica-de-simulação)
- [Pré-requisitos](#pré-requisitos)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Rodando com Docker](#rodando-com-docker)
- [Rodando localmente](#rodando-localmente)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Payments |
| Tipo | Worker Service (sem HTTP endpoints) |
| Banco de dados | PostgreSQL — `fcg_payments_db` (opcional: InMemory para simulação) |
| Consome evento | `OrderPlacedEvent` ← RabbitMQ |
| Publica evento | `PaymentProcessedEvent` → RabbitMQ |

---

## Arquitetura

Serviço do tipo **Worker Service** — sem controllers ou endpoints HTTP. Toda comunicação ocorre via eventos RabbitMQ usando MassTransit.

```
fcg-payments-api/
└── src/
    ├── FCG.Payments.Domain        # Entidade Payment, enum PaymentStatus
    ├── FCG.Payments.Application   # PaymentService (lógica de simulação)
    └── FCG.Payments.Worker        # OrderPlacedConsumer, Program.cs
```

### Padrões aplicados

- **Event-Driven** — reage exclusivamente a eventos; não expõe HTTP
- **Consumer Pattern** — `OrderPlacedConsumer` processa a mensagem, chama `PaymentService` e publica o resultado
- **Simulação controlada** — aprovação/rejeição configurável (padrão: 90% aprovado)

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Tipo de projeto | Worker Service | — |
| ORM | Entity Framework Core + Npgsql | 10.0.5 |
| Banco de Dados | PostgreSQL | 16 |
| Mensageria | MassTransit + RabbitMQ | — |
| Logging | Serilog | 10.0.0 |

---

## Domínio

### Entidade Payment

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | Guid | Identificador único |
| `OrderId` | Guid | Referência ao pedido originado no CatalogAPI |
| `UserId` | Guid | Referência ao usuário |
| `GameId` | Guid | Referência ao jogo |
| `Amount` | decimal | Valor processado |
| `Status` | enum | `Approved` ou `Rejected` |
| `ProcessedAt` | DateTime | Data/hora do processamento |

### Enum PaymentStatus

```csharp
public enum PaymentStatus { Approved, Rejected }
```

---

## Fluxo de Mensagens

```
CatalogAPI
    │
    │  OrderPlacedEvent  →  [order.placed]
    │
    ▼
PaymentsAPI (OrderPlacedConsumer)
    │
    │  chama PaymentService.ProcessAsync()
    │
    │  PaymentProcessedEvent  →  [payment.processed]
    │
    ├──▶  CatalogAPI (PaymentProcessedConsumer)
    │         └── Status=Approved → adiciona jogo à biblioteca
    │
    └──▶  NotificationsAPI (PaymentProcessedConsumer)
              └── Status=Approved → loga e-mail de confirmação
```

### OrderPlacedEvent (consumido)

```csharp
public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
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
    public PaymentStatus Status { get; init; }  // Approved | Rejected
    public string? Reason { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

---

## Lógica de Simulação

O `PaymentService` simula aprovação/rejeição sem integração com gateway real:

```csharp
// 90% de chance de aprovação, 10% de rejeição
var approved = new Random().NextDouble() > 0.1;
return new PaymentResult(
    Status: approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
    Reason: approved ? null : "Insufficient funds (simulated)"
);
```

> Para demonstrações, a lógica pode ser alterada para **sempre aprovar** via variável de ambiente `Payments__ApprovalRate=1.0`.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — RabbitMQ via Docker Compose (repositório `fcg-infra`)

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL | `Host=postgres;Port=5432;Database=fcg_payments_db;Username=fcg;Password=fcg_secret` |
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |
| `Payments__ApprovalRate` | Taxa de aprovação (0.0 a 1.0) | `0.9` |

---

## Rodando com Docker

Suba toda a infraestrutura a partir do repositório `fcg-infra`:

```bash
docker compose up -d
```

O worker iniciará automaticamente e ficará aguardando eventos no RabbitMQ.

Acompanhe os logs em tempo real:

```bash
docker logs -f fcg_payments_worker
```

---

## Rodando localmente

### 1. Configure o RabbitMQ (e opcionalmente o PostgreSQL)

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
docker run -d --name postgres -e POSTGRES_USER=fcg -e POSTGRES_PASSWORD=fcg_secret -e POSTGRES_DB=fcg_payments_db -p 5432:5432 postgres:16-alpine
```

### 2. Restaure dependências

```bash
dotnet restore
```

### 3. Execute o Worker

```bash
dotnet run --project src/FCG.Payments.Worker/FCG.Payments.Worker.csproj
```

---

## Estrutura do Projeto

```
src/
├── FCG.Payments.Domain/
│   ├── Entities/            # Payment
│   └── Enums/               # PaymentStatus
│
├── FCG.Payments.Application/
│   ├── Services/            # PaymentService
│   └── Interfaces/          # IPaymentService
│
└── FCG.Payments.Worker/
    ├── Consumers/           # OrderPlacedConsumer
    ├── Program.cs
    └── appsettings.json
```

---

## Logs

Todos os eventos processados são registrados via **Serilog**:

| Evento | Nível | Mensagem |
|---|---|---|
| Pagamento aprovado | Information | `[PAYMENT] OrderId={id} aprovado. Valor: R$ {amount}` |
| Pagamento rejeitado | Warning | `[PAYMENT] OrderId={id} rejeitado. Motivo: {reason}` |
| Erro no consumer | Error | Stack trace completo |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
