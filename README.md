# credito-proposta (Proposta de Crédito)

Microsserviço que **consome** `ClienteCadastrado`, calcula score, persiste a proposta e publica `PropostaGerada` (sempre, inclusive quando negada).

## Stack

- .NET 10 / ASP.NET Core
- EF Core + SQL Server (database `PropostaCredito`)
- RabbitMQ (consumer + outbox publisher)
- Idempotência de consumidor (`processed_events` + unique `clienteId`)
- DLQ: `proposta.cliente-cadastrado.dlq`
- xUnit + FluentAssertions + NSubstitute

## Regras de score

| Score | Status | Cartões | Limite |
|------|--------|---------|--------|
| 0–100 | Negada | 0 | — |
| 101–500 | Aprovada | 1 | R$ 1.000 |
| 501–1000 | Aprovada | 2 | R$ 5.000 cada (total R$ 10.000) |

Score = `long.Parse(cpf) % 1001` (determinístico).

## Subir

1. Infra compartilhada (Cadastro):

```bash
cd ../CadastroCliente/CadastroCliente
docker compose up -d
```

2. API Proposta:

```bash
cd ../../PropostaCredito
docker compose up -d --build
```

Ou local:

```bash
dotnet run --project Api
```

| Serviço | URL |
|---------|-----|
| API Proposta | http://localhost:8081 |
| Health | http://localhost:8081/health |
| OpenAPI | http://localhost:8081/openapi/v1.json |
| GET proposta | `GET /api/v1/propostas/{clienteId}` |

## Fluxo

```
ClienteCadastrado (credito.clientes)
  → fila proposta.cliente-cadastrado
  → score + proposta (idempotente)
  → outbox
  → PropostaGerada (credito.propostas)
```

## Decisões

- Idempotência: `eventId` em `processed_events` + 1 proposta por `clienteId`.
- `PropostaGerada` também na negação (Cartão decide não emitir).
- Mesmo Rabbit/SQL do Cadastro; database separado (`PropostaCredito`).
