# Warranty & Repair Ledger

Local-first web API + helper UI for tracking purchases, warranty windows, and repair attempts, built to model Danish/EU consumer rules around warranty and the 2024 "Right to Repair" update.

## Why this exists (2-minute pitch)

- Consumers in DK/EU have a **2-year legal guarantee** as baseline. If a consumer **chooses repair** while in warranty, the **legal guarantee is extended by 12 months** under the EU's 2024 Right-to-Repair directive.
- This repo encodes those rules into a small, testable .NET 8 API with an optional React UI so reviewers can exercise the model quickly.

*Sources: EU Commission (Right to Repair, 2024), Forbrug.dk (2-årig reklamationsret), Reuters coverage of the 12-month extension.*

## Features

- Create/list products with purchase date and warranty length
- Track repair cases per product with clear status transitions
- "Expiring soon" view (warranties ending in the next N days)
- Summary report (counts by status, average repair duration)
- Local **SQLite** database; runs entirely offline
- **Swagger/OpenAPI** for exploration
- **Integration tests** with xUnit + `WebApplicationFactory`
- Optional **Vite + React** helper UI to poke at the API without REST tooling

## Quick start

### A) Docker Compose (fastest)

```bash
docker compose up --build
# API: http://localhost:8080/swagger
```

### B) VS Code Dev Container

1. Requirements: Docker Desktop and the *Dev Containers* extension.
2. In VS Code: **Dev Containers: Open Folder in Container…**
3. Run the API inside the container:

   ```bash
   dotnet watch run --project WarrantyRepairLedger/WarrantyRepairLedger.csproj --urls http://0.0.0.0:8080
   # Swagger on http://localhost:8080/swagger
   ```

4. Exercise endpoints with the included REST file: open `requests.http` and click **Send Request** on each block.

### (Optional) Run the helper UI

Keep the API running on port 8080, then:

```bash
cd frontend
npm install
npm run dev
# Vite dev server on http://localhost:5173 (CORS preconfigured)
```

## Tests & coverage

```bash
dotnet test
# or with coverage (lcov):
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./TestResults/coverage/ /p:CoverletOutputFormat=lcov
```

**Current scope:** warranty math, status transitions, expiring-soon query, validation + RFC7807, and an end-to-end happy path.

## Data model (EF Core)

```
Product: Id, Name, Brand, Serial (unique), PurchaseDate, WarrantyMonths (default 24), Retailer, Price?
Repair: Id, ProductId (FK), OpenedAt, ClosedAt?, Status (Open|InProgress|Fixed|Rejected), Cost?, Notes, ConsumerOptedForRepair?
Indexes: IX_Product_Serial (unique), IX_Repair_ProductId_Status
```

## API (highlights)

- `POST /products` - create
- `GET /products` - list + `q` filter
- `GET /products/{id}/in-warranty` - boolean + reason
- `GET /products/expiring?days=30` - expiring soon
- `POST /repairs` - open
- `PATCH /repairs/{id}` - transition (`Open → InProgress → Fixed|Rejected`)
- `GET /reports/summary` - counts/avg/soon-to-expire

## Validation & errors

DTOs use data annotations (e.g., `[Required]`, `[Range]`). Invalid payloads return **RFC 7807** `application/problem+json`. Unhandled exceptions are converted to a standardized Problem Details payload (includes a `traceId` in Development/Testing). Hitting `GET /diagnostics/throw` in dev intentionally raises an exception so you can trace the error contract end-to-end.

## Warranty assumptions

- Default coverage: **24 months** from purchase (Danish/EU legal guarantee).
- If a consumer **opts for repair** (not replacement) during the warranty, completed repairs extend the legal guarantee by **12 months** (Right-to-Repair, 2024).
- The tracker doesn't model component-level identities; it assumes distinct components across repairs (so it does not restart a fresh 24-month clock for the same exact part).

> **Disclaimer:** This project is for learning/demo purposes, not legal advice.

## Architecture notes

- .NET 8 Minimal API, EF Core + SQLite (file DB under `./data`)
- Migrations included; Dev Container applies them automatically on first run
- Tests: xUnit + `WebApplicationFactory` (SQLite in-memory)
- CORS: `LedgerFrontend` policy whitelists Vite dev/preview ports on localhost