# credito-proposta (Proposta de Crédito)

Microsserviço que **consome** `ClienteCadastrado`, calcula score, persiste a proposta e publica `PropostaGerada` (sempre, inclusive quando negada).

## Processos

| Projeto | Responsabilidade |
|---------|------------------|
| **Api** | REST `GET /api/v1/propostas/{clienteId}` + health |
| **Worker** | Consumer RabbitMQ + outbox publisher |

## Stack

- .NET 10 / ASP.NET Core + Worker Service
- EF Core + SQL Server (database `PropostaCredito`)
- RabbitMQ (consumer com DLQ + outbox)
- Idempotência (`processed_events` + unique `clienteId`)
- xUnit + FluentAssertions + NSubstitute

## Regras de score

| Score | Status | Cartões | Limite |
|------|--------|---------|--------|
| 0–100 | Negada | 0 | — |
| 101–500 | Aprovada | 1 | R$ 1.000 |
| 501–1000 | Aprovada | 2 | R$ 5.000 cada (total R$ 10.000) |

Score = `long.Parse(cpf) % 1001` (determinístico).

## Subir

1. Infra (Cadastro):

```bash
cd ../CadastroCliente/CadastroCliente
docker compose up -d
```

2. Api + Worker:

```bash
cd ../../PropostaCredito
docker compose up -d --build
```

Local:

```bash
dotnet run --project Api
dotnet run --project Worker
```

| Serviço | URL |
|---------|-----|
| API Proposta | http://localhost:8081 |
| Health | http://localhost:8081/health |
| OpenAPI | http://localhost:8081/openapi/v1.json |

## Fluxo

```
ClienteCadastrado → Worker (fila + DLQ) → proposta idempotente → outbox
  → PropostaGerada → RabbitMQ
Api apenas consulta GET /propostas/{clienteId}
```

## Decisões

- **Api ≠ Worker**: REST e mensageria escalam separados.
- Idempotência: `eventId` + 1 proposta por `clienteId`.
- `PropostaGerada` também na negação.
- Mesmo Rabbit/SQL do Cadastro; database `PropostaCredito`.
