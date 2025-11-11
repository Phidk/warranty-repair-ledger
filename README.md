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

## Tech Stack

- **Backend:** .NET 8 Minimal API (C#), EF Core (SQLite)
- **Docs:** Swagger / OpenAPI (dev-only)
- **Testing:** xUnit, Microsoft.AspNetCore.Mvc.Testing
- **Tooling:** C# VS Code extension, .NET CLI

## Quick Start

### Prerequisites
- .NET SDK 8.x  
- VS Code with the **C#** extension installed  
- SQLite (optional; DB file is created automatically)

### 1) Clone & run
```bash
git clone https://github.com/<you>/warranty-repair-ledger.git
cd warranty-repair-ledger
dotnet restore
dotnet build
```

### 2) Configure connection string
Create or edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=./data/ledger.db"
  },
  "Warranty": {
    "DefaultMonths": 24
  },
  "AllowedHosts": "*"
}
```

### 3) Add EF Core & create database
```bash
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Swashbuckle.AspNetCore
mkdir -p data

# Create initial migration and update DB
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4) Run
```bash
dotnet run
```
- API listens on `https://localhost:7047` (or similar).  
- Swagger UI (dev only): `https://localhost:7047/swagger`

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

## Requests (REST Client)

Create `requests.http` in repo root:

```http
### Create product
POST https://localhost:7047/products
Content-Type: application/json

{
  "name": "Phone X",
  "brand": "Acme",
  "serial": "SN123",
  "purchaseDate": "2024-09-15",
  "warrantyMonths": 24
}

### Get expiring-in-30-days
GET https://localhost:7047/products/expiring?days=30

### Open a repair
POST https://localhost:7047/repairs
Content-Type: application/json

{
  "productId": 1,
  "openedAt": "2025-01-10T09:30:00Z",
  "status": "Open",
  "notes": "Screen flicker"
}

### Summary
GET https://localhost:7047/reports/summary
```

## Testing

```bash
# create test project once
dotnet new xunit -n WarrantyRepairLedger.Tests
dotnet add WarrantyRepairLedger.Tests/WarrantyRepairLedger.Tests.csproj reference WarrantyRepairLedger.csproj
dotnet add WarrantyRepairLedger.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add WarrantyRepairLedger.Tests package Microsoft.EntityFrameworkCore.Sqlite

# run tests
dotnet test
```

## Project Structure

```
/src
  Program.cs
  Models/
  Data/ (DbContext, Migrations/)
  Endpoints/ or Controllers/
  Services/
  appsettings.json
/tests
  WarrantyRepairLedger.Tests/
data/ (ledger.db)
requests.http
```

## Roadmap

- Attach receipts (file upload)
- Export CSV