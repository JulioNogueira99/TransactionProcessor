# TransactionProcessor — Desafio Técnico PagueVeloz (cliente Serasa) — Meta

![Net Core](https://img.shields.io/badge/.NET%209-512BD4?style=flat&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=flat&logo=microsoft-sql-server&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-xUnit-success)

API REST em **.NET 9** para processamento de transações financeiras com **consistência**, **concorrência**, **idempotência**, **resiliência** e **processamento assíncrono via Outbox**.

> ✅ Entrega pensada para o avaliador rodar com **1 comando**: `docker compose up --build` e acessar o **Swagger**.

---

## Sumário

- [Visão geral](#visão-geral)
- [Requisitos atendidos](#requisitos-atendidos)
- [Arquitetura](#arquitetura)
- [Tecnologias e bibliotecas](#tecnologias-e-bibliotecas)
- [Modelo de domínio](#modelo-de-domínio)
- [Validações de negócio](#validações-de-negócio)
- [Como executar](#como-executar)
- [Via Docker Compose (recomendado)](#via-docker-compose-recomendado)
- [Localmente (sem Docker)](#localmente-sem-docker)
- [Migrations e banco](#migrations-e-banco)
- [Health checks](#health-checks)
- [Testes e cobertura](#testes-e-cobertura)
- [Exemplos de uso da API](#exemplos-de-uso-da-api)
- [Erros e códigos HTTP](#erros-e-códigos-http)
- [Notas sobre concorrência, resiliência e Outbox](#notas-sobre-concorrência-resiliência-e-outbox)
- [Exportar solução anonimizada (git archive)](#exportar-solução-anonimizada-git-archive)
- [Licença](#licença)

---

## Visão geral

O **TransactionProcessor** é uma API que gerencia **clientes (Customer)**, **contas (Account)** e **transações**:

- `credit`
- `debit`
- `reserve`
- `capture`
- `transfer` (entre contas do mesmo cliente)

Garantias principais:

- **Consistência transacional:** gravação de transação + outbox no mesmo commit.
- **Concorrência:** suporta múltiplas requisições concorrentes sem corromper saldo.
- **Idempotência:** reenvio da mesma requisição (mesmo `reference_id`) não duplica transações.
- **Resiliência:** retries controlados onde é seguro repetir; outbox com tentativas e agendamento.
- **Processamento assíncrono:** eventos persistidos na Outbox e processados por um worker.

---

## Requisitos atendidos

### Entregáveis (PDF)

- ✅ Repositório Git público com histórico de commits
- ✅ README detalhado (este arquivo)
- ✅ Código em C# (.NET 9)
- ✅ Testes automatizados
- ✅ OpenAPI/Swagger
- ✅ (Opcional) **Docker Compose** para executar tudo com 1 comando

### Aspectos técnicos avaliados

- ✅ **Concorrência** (RowVersion + lock por conta)
- ✅ **Idempotência** (`reference_id`) protegida por índice único
- ✅ **Outbox Pattern** (processamento confiável assíncrono)
- ✅ **Observabilidade** (logs e rastreabilidade por transação)
- ✅ **Validação de entrada** (FluentValidation) + ProblemDetails
- ✅ **Performance** (índices e queries previsíveis; lock apenas quando necessário)

---

## Arquitetura

Estrutura em camadas seguindo **Clean Architecture** (dependências sempre “para dentro”):

```
src/
  TransactionProcessor.Domain          -> Entidades, Enums, Regras de negócio puras
  TransactionProcessor.Application     -> Casos de uso/Services, Interfaces (ports), DTOs, Validators
  TransactionProcessor.Infrastructure  -> EF Core, Repositories, Outbox, Lock (sp_getapplock), DbContext
  TransactionProcessor.Api             -> Controllers, DI, Swagger, HealthChecks
tests/
  TransactionProcessor.UnitTests
```

### Por que as interfaces ficam na Application?

A **Application** define as “portas” (ex.: `IAccountRepository`, `IUnitOfWork`, `IOutboxStore`, `IAccountLock`) e a **Infrastructure** implementa (adaptadores).  
Assim, o núcleo do sistema (**Domain + Application**) não depende de detalhes de banco/EF.

---

## Tecnologias e bibliotecas

- **.NET 9 / ASP.NET Core Web API** — base da API, DI, hosted services
- **EF Core + SQL Server** — persistência relacional, migrations, concorrência otimista (RowVersion)
- **Swashbuckle (Swagger/OpenAPI)** — documentação interativa
- **FluentValidation** — validação rigorosa de entrada
- **xUnit + Moq** — testes unitários
- **coverlet.collector** — coleta de cobertura (CI-friendly)
- **Docker + Docker Compose** — execução plug-and-play (opcional no desafio, entregue aqui)

**Decisão importante:** o controle de concorrência combina:
- **RowVersion** (otimista) para detectar conflitos de escrita
- **Lock por conta** no SQL Server (`sp_getapplock`) para serializar operações críticas por conta (e evitar double-spend)

---

## Modelo de domínio

### Account

- `Balance`, `ReservedBalance`, `CreditLimit`
- `CashAvailable = Balance - ReservedBalance`
- `AvailableBalance = CashAvailable + CreditLimit`
- `RowVersion` para concorrência otimista
- `Status`: `Active`, `Inactive`, `Blocked` (conforme implementação)
- Vinculada a um `Customer` (um cliente pode ter N contas)

### Customer

- Entidade do cliente (identificação por GUID no domínio)
- Pode possuir múltiplas contas

### Transaction

- Associada a uma conta
- Tipos: `credit`, `debit`, `reserve`, `capture`, `transfer`
- `reference_id`: chave de idempotência

#### Transfer (auditoria completa com duas legs)

A transferência grava **duas transações** com o mesmo `reference_id`:

- **leg = 1** → débito na conta origem (counterparty = destino)
- **leg = 2** → crédito na conta destino (counterparty = origem)

Operações “single-account” usam **leg = 0**.

Índice único:
- **(ReferenceId, Leg)**

---

## Validações de negócio

Regras do PDF implementadas no domínio:

- Operações **não podem deixar o `AvailableBalance` negativo**
- Limite de crédito deve ser respeitado
- Débito considera **saldo disponível + limite de crédito**
- Reservas só podem ser feitas com **saldo disponível (cash)** (`CashAvailable`)
- Capturas só podem ser feitas com **saldo reservado suficiente** (`ReservedBalance`)

---

## Como executar

### Via Docker Compose (recomendado)

### Pré-requisitos
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando.

### Passo a Passo

1.  **Clone o repositório:**
    ```bash
    git clone https://github.com/JulioNogueira99/TransactionProcessor.git
    cd TransactionProcessor
    ```

2.  **Suba o ambiente:**
    Execute o comando abaixo na raiz do projeto. Ele irá compilar a API, baixar o SQL Server e configurar a rede.
    ```bash
    docker compose up --build
    ```

A API ficará disponível em:
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

Banco SQL Server:
- Host: `localhost,1433`
- Usuário: `sa`
- Senha: definida no `docker-compose.yml` (ex.: `Challenge_PagueVeloz123!`)
- Database: `PagueVelozChallenge` (ou conforme compose)

Para parar:

```bash
docker compose down
```

> Observação: o compose usa `ASPNETCORE_ENVIRONMENT=Development` para manter Swagger habilitado e evitar redirecionamento HTTPS no container.

---

### Localmente (sem Docker)

Pré-requisitos:
- .NET SDK 9
- SQL Server/LocalDB/SQL Express disponível

1) Configure a connection string (`DefaultConnection`) em:
- `src/TransactionProcessor.Api/appsettings.Development.json`
ou via variável de ambiente:

PowerShell:
```powershell
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\mssqllocaldb;Database=TransactionProcessor;Trusted_Connection=True;TrustServerCertificate=True;"
```

2) Rode migrations (se sua API não estiver auto-migrating neste ambiente):
```bash
dotnet ef database update -p src/TransactionProcessor.Infrastructure -s src/TransactionProcessor.Api
```

3) Rode a API:
```bash
dotnet run --project src/TransactionProcessor.Api
```

---

## Migrations e banco

### Criar migration
```bash
dotnet ef migrations add <NomeDaMigration> -p src/TransactionProcessor.Infrastructure -s src/TransactionProcessor.Api
```

### Aplicar migrations
```bash
dotnet ef database update -p src/TransactionProcessor.Infrastructure -s src/TransactionProcessor.Api
```

> No Docker, as migrations são aplicadas automaticamente no startup (para facilitar avaliação).

---

## Health checks

Endpoint:
- `GET /health`

Inclui:
- ✅ check do DbContext / SQL Server (quando registrado)

---

## Testes e cobertura

### Rodar testes
Na raiz do repo:

```bash
dotnet test
```

### Cobertura (coverlet collector)
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Exemplos de uso da API

> Os exemplos abaixo são para uso no Swagger.  
> Substitua os GUIDs por IDs reais do seu banco.

### 1) Criar conta (cria o cliente se não existir)

`POST /api/accounts`

Request:
```json
{
  "client_id": "CLI-001",
  "credit_limit": 100.00
}
```

Response `201 Created`:
```json
{
  "account_id": "8db3b5c0-cc9b-4b88-9b5a-0a0f3a26efbe",
  "balance": 0,
  "reserved_balance": 0,
  "available_balance": 100.0,
  "credit_limit": 100.0
}
```

---

### 2) Consultar conta

`GET /api/accounts/{id}`

Response `200 OK`:
```json
{
  "account_id": "8db3b5c0-cc9b-4b88-9b5a-0a0f3a26efbe",
  "balance": 10,
  "reserved_balance": 0,
  "available_balance": 110,
  "credit_limit": 100.0
}
```

Response `404 Not Found` (ProblemDetails):
```json
{
  "title": "Account not found",
  "status": 404
}
```

---

### 3) Processar transação (credit/debit/reserve/capture)

`POST /api/transactions`

**Credit**
```json
{
  "account_id": "11111111-1111-1111-1111-111111111111",
  "operation": "credit",
  "amount": 50,
  "currency": "BRL",
  "reference_id": "REF-CREDIT-001"
}
```

Response `200 OK`:
```json
{
  "transaction_id": "f4b43341-e44f-41d6-9ba3-893293566197",
  "status": "success",
  "balance": 50,
  "reserved_balance": 0,
  "available_balance": 1550,
  "timestamp": "2026-02-01T00:00:00Z",
  "error_message": null
}
```

✅ **Idempotência:** reenviar o mesmo JSON com o mesmo `reference_id` retorna o mesmo resultado (não duplica).

---

### 4) Transferência entre contas do mesmo cliente (Caso #4)

`POST /api/transactions`

```json
{
  "account_id": "11111111-1111-1111-1111-111111111111",
  "destination_account_id": "22222222-2222-2222-2222-222222222222",
  "operation": "transfer",
  "amount": 10,
  "currency": "BRL",
  "reference_id": "TRF-0001"
}
```

Regras:
- Origem e destino devem ser do mesmo cliente
- Locks são adquiridos em ordem determinística para evitar deadlock
- Auditoria gera duas transactions (**leg 1 e leg 2**) com a mesma `reference_id`

---

## Erros e códigos HTTP

A API retorna erros no formato **ProblemDetails**.

- `400 Bad Request` — validação de entrada (FluentValidation) / payload inválido
- `404 Not Found` — recurso inexistente (ex.: conta não encontrada)
- `409 Conflict` — conflito de concorrência (quando aplicável)
- `422 Unprocessable Entity` — regra de negócio (ex.: saldo insuficiente)
- `503 Service Unavailable` — falhas temporárias (quando aplicável)

Exemplo: erro de validação (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "currency": ["currency must be uppercase (e.g. BRL, USD)."]
  }
}
```

---

## Notas sobre concorrência, resiliência e Outbox

### Concorrência

- **Optimistic concurrency** via `RowVersion`
- **Lock por conta** via `sp_getapplock` com `LockOwner='Transaction'` para serializar operações críticas
- Para `transfer`, locks são adquiridos na mesma ordem (menor GUID → maior GUID), reduzindo risco de deadlock

### Resiliência

- Retries aplicados onde é seguro repetir (ex.: conflitos de concorrência e fluxo do outbox)
- O worker de Outbox registra tentativas e agenda próximas execuções (backoff)

### Outbox

- A transação de domínio e a escrita da mensagem de outbox acontecem no **mesmo commit**
- Um `BackgroundService`/worker processa mensagens pendentes e marca como processadas
- Isso evita perder eventos em caso de falha entre “persistir no banco” e “publicar”

---

## Licença

Projeto de desafio técnico — uso livre para avaliação.
