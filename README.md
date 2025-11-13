# Warranty & Repair Ledger

Local-first web API + helper UI for tracking purchases, warranty windows, and repair attempts, built to model Danish/EU consumer rules around warranty and the 2024 "Right to Repair" update.

## Why this exists

European consumers technically enjoy a two year legal guarantee, yet most people lose track of receipts, warranty windows, and the extra protection that kicks in when they choose repair. The 2024 Right to Repair update adds another 12 months of coverage after a successful repair, so a household that tracks repairs can squeeze one more season out of an appliance before replacing it.

This project keeps that bookkeeping boring on purpose: a local first API, a helper UI, and a test suite that encodes the Danish and EU timelines without needing a spreadsheet. Point it at a SQLite file, capture repairs as they happen, and you always know if a seller must fix or refund the item.

### Sources and policy links

- [EU Commission Right to Repair overview](https://commission.europa.eu/law/law-topic/consumer-protection-law/directive-repair-goods_en)
- [Forbrug.dk explainer on the two year warranty](https://forbrug.dk/regler/reklamationsret-i-2-aar)
- [Reuters coverage of the 12 month extension proposal](https://www.reuters.com/sustainability/eu-parliament-approves-rules-requiring-companies-repair-worn-out-products-2024-04-23/)

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

**Dev Container bootstrap:** The `.devcontainer/post-create.sh` hook fixes folder ownership, removes stale `bin` and `obj` outputs, restores tools, and applies migrations so the SQLite file is in sync with the current models.

### (Optional) Run the helper UI

Keep the API running on port 8080, then:

```bash
cd frontend
npm install
npm run dev
# Vite dev server on http://localhost:5173 (CORS preconfigured)
```

### Reset the local database

Outside the Dev Container you can mirror the bootstrap script to clear compiled artifacts and recreate the SQLite database:

```bash
rm -rf \
  WarrantyRepairLedger/bin \
  WarrantyRepairLedger/obj \
  tests/WarrantyRepairLedger.Tests/bin \
  tests/WarrantyRepairLedger.Tests/obj
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project WarrantyRepairLedger/WarrantyRepairLedger.csproj
```

Running those commands gives you the same clean state the container receives after `post-create`.

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
- **Replacement not modelled:** Under Danish rules, if a seller replaces a defective item ("omlevering"), the consumer normally gets a new two-year complaint period from the replacement date. This ledger does not attempt to track that automatically; treat a full replacement as a new product entry with its own purchase date/warranty window.

> **Legal status (Denmark / EU):** The 12‑month extension modelled here comes from Directive (EU) 2024/1799 on common rules promoting the repair of goods, which EU Member States must transpose and start applying from **31 July 2026**. As of November 2025, Denmark has **not yet implemented** the directive in national law. The government's 2025-26 legislative programme announces a forthcoming **"Lov om reparation af varer"** to implement the directive and adjust the Danish *købelov* (including complaint periods after repair), but the final Danish choices and entry-into-force date are still pending. This ledger therefore encodes the directive's minimum rule set (extra 12 months after a repair) as a forward‑looking model, not a statement of currently enforceable Danish law.


> **Disclaimer:** This project is for learning/demo purposes, not legal advice. It models the EU Right-to-Repair regime (Directive (EU) 2024/1799) in a Danish/EU context, but does not track every national variation (e.g., detailed replacement rules, second-hand goods, or future implementation choices).

## Architecture notes

- .NET 8 Minimal API, EF Core + SQLite (file DB under `./data`)
- Migrations included; Dev Container applies them automatically on first run
- Tests: xUnit + `WebApplicationFactory` (SQLite in-memory)
- CORS: `LedgerFrontend` policy whitelists Vite dev/preview ports on localhost