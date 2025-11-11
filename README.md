# Warranty & Repair Ledger

Minimal local-first web app for tracking purchases, warranty windows, and repair attempts.

## Features

- Create/list products with purchase date and warranty length
- Track repair cases per product with clear status transitions
- “Expiring soon” view (e.g., warranties ending in the next N days)
- Summary report: counts by status, average repair duration
- Local SQLite database; runs entirely offline
- Swagger/OpenAPI docs for quick exploration
- xUnit integration tests using an in-memory SQLite database
- Warranty defaults model the Danish Sale of Goods Act two year period; the sample implementation assumes failures are on different parts, so it does not grant an additional two year term for the exact same component

## Tech Stack

- **Backend:** .NET 8 Minimal API (C#), EF Core (SQLite)
- **Docs:** Swagger / OpenAPI (dev-only)
- **Testing:** xUnit, Microsoft.AspNetCore.Mvc.Testing
- **Tooling:** C# VS Code extension, .NET CLI

## Quick Start - VS Code Dev Container only

This project is tuned for the VS Code Dev Container workflow so you never have to install .NET, SQLite, or `dotnet-ef` on your host.

1. **Requirements**  
   - Docker Desktop (or another Docker Engine)  
   - VS Code + the **Dev Containers** extension

2. **Open inside the container**  
   - Clone the repo locally and run “Dev Containers: Open Folder in Container…”  
   The `.devcontainer` image already includes .NET 8 and SQLite; a post-create script resets file permissions/cleans `bin`+`obj`, runs `dotnet restore`, `dotnet tool restore`, and applies the EF Core migration. A named Docker volume (`<repo>-data`) persists `data/ledger.db`. If you pull this repo after the container already exists, run **Dev Containers: Rebuild Container** so the script re-executes.

3. **Run the API** (inside the Dev Container terminal)  
   ```bash
   dotnet watch run --project WarrantyRepairLedger/WarrantyRepairLedger.csproj --urls http://0.0.0.0:8080
   ```
   Forwarded port 8080 will appear in the VS Code Ports panel. Click the forwarding link (or open `http://localhost:8080/swagger`) to see the Swagger UI; hitting the bare root (`/`) returns 404 because no endpoint is mapped there.

4. **Exercise the endpoints**  
   - Open `requests.http` in VS Code; every block already points at `http://localhost:8080`.  
   - The Dev Container pre-installs the REST Client extension, so click “Send Request” above each block to walk the full flow: create product → search/filter → expiring soon → warranty check → create repair → advance repair → list open repairs → summary report.

5. **Run the automated tests** (still inside the container)  
   ```bash
   dotnet test
   ```
   This covers warranty math, repair transitions, expiring queries, and the integration path end-to-end.

## Data Model (EF Core)

```
Product
- Id (int, PK)
- Name (string, required)
- Brand (string)
- Serial (string, unique index)
- PurchaseDate (date)
- WarrantyMonths (int, default 24)
- Retailer (string)
- Price (decimal?)

Repair
- Id (int, PK)
- ProductId (FK -> Product.Id)
- OpenedAt (datetime)
- ClosedAt (datetime?)
- Status (enum: Open, InProgress, Fixed, Rejected)
- Cost (decimal?)
- Notes (string)
Indexes:
- IX_Product_Serial (unique)
- IX_Repair_ProductId_Status
```

## API

### Products
- `POST /products` – create a product  
  ```json
  {
    "name": "Phone X",
    "brand": "Acme",
    "serial": "SN123",
    "purchaseDate": "2024-09-15",
    "warrantyMonths": 24,
    "retailer": "Example Store",
    "price": 399.99
  }
  ```
- `GET /products/{id}` – single product
- `GET /products` – list products (optional `q` to filter by name/serial)
- `GET /products/expiring?days=30` – warranties ending within N days
- `GET /products/{id}/in-warranty` – boolean with reason

### Repairs
- `POST /repairs` – create a repair  
  ```json
  {
    "productId": 1,
    "openedAt": "2025-01-10T09:30:00Z",
    "status": "Open",
    "notes": "Screen flicker"
  }
  ```
- `PATCH /repairs/{id}` – transition status (`Open -> InProgress -> Fixed|Rejected`)
- `GET /repairs?status=Open` – filter by status

### Reports
- `GET /reports/summary` – counts per status, avg days open, soon-to-expire count

## Validation & Errors

- DTOs enforce `[Required]`, `[Range]`, and similar data annotations; invalid payloads return `400` responses serialized as RFC 7807 `application/problem+json` with the `errors` dictionary populated per field.
- Unhandled exceptions are caught by a centralized `ProblemDetailsExceptionHandler`, producing a `500` RFC 7807 payload that includes a `traceId` extension for correlating logs.
- When running in Development or Testing (including the Dev Container), hitting `GET /diagnostics/throw` intentionally raises an exception so you can verify the error contract end-to-end.

## Warranty assumptions

- Default coverage is 24 months from purchase, aligning with the Danish Sale of Goods Act (Købeloven §§78-81) and EU directive 2019/771
- The tracker does not capture part-level identifiers, so it assumes each repair targets a different component and therefore does not start a fresh two year clock for the same part

## Requests (REST Client)

`requests.http` lives at the repo root and covers the happy path:
- create/search products
- view expiring warranties + warranty status
- open a repair, advance its status, and list open repairs
- pull `/reports/summary`

It defaults to `http://localhost:8080`, matching the Dev Container port mapping.

Open the file in VS Code with the REST Client extension to run each request against your dev server.

## Testing

```bash
dotnet test
```

Tests cover:
- warranty math edge cases (default months + expiry boundaries)
- repair status transition rules
- expiring-products query
- validation + RFC 7807 responses (including the diagnostics exception endpoint)
- end-to-end flow: product → repair → summary report

## Project Structure

```
.
├── WarrantyRepairLedger.sln
├── WarrantyRepairLedger/
│   ├── Data/
│   ├── DTOs/
│   ├── Endpoints/
│   ├── Migrations/
│   ├── Models/
│   ├── Options/
│   ├── Serialization/
│   ├── Services/
│   ├── Program.cs
│   └── appsettings*.json
├── tests/
│   └── WarrantyRepairLedger.Tests/ (xUnit + WebApplicationFactory)
├── data/ (SQLite DB lives here)
├── requests.http
└── .config/dotnet-tools.json (local dotnet-ef manifest)
```

## Roadmap

- Attach receipts (file upload)
- Export CSV
