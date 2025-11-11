#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="/workspaces/warranty-repair-ledger"

sudo chown -R vscode:vscode "$WORKSPACE"

cd "$WORKSPACE"

rm -rf \
  WarrantyRepairLedger/bin \
  WarrantyRepairLedger/obj \
  tests/WarrantyRepairLedger.Tests/bin \
  tests/WarrantyRepairLedger.Tests/obj || true

dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project WarrantyRepairLedger/WarrantyRepairLedger.csproj
