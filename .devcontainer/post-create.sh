#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEFAULT_WORKSPACE="/workspaces/warranty-repair-ledger"
WORKSPACE="${WORKSPACE:-$DEFAULT_WORKSPACE}"

if [ ! -d "$WORKSPACE" ]; then
  WORKSPACE="$SCRIPT_DIR"
fi

if [ "$WORKSPACE" = "$DEFAULT_WORKSPACE" ] && command -v sudo >/dev/null 2>&1; then
  if sudo -n true >/dev/null 2>&1; then
    sudo chown -R vscode:vscode "$WORKSPACE"
  else
    echo "Skipping chown; sudo requires a password." >&2
  fi
fi

cd "$WORKSPACE"

rm -rf \
  WarrantyRepairLedger/bin \
  WarrantyRepairLedger/obj \
  tests/WarrantyRepairLedger.Tests/bin \
  tests/WarrantyRepairLedger.Tests/obj || true

dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project WarrantyRepairLedger/WarrantyRepairLedger.csproj
