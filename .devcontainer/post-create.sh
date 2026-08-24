#!/usr/bin/env bash
# Tao DB tren SQL Server sidecar bang EF (repo khong commit Migrations).
set -uo pipefail
cd /workspace
export PATH="$PATH:$HOME/.dotnet/tools"

echo "[ef] cai dotnet-ef..."
dotnet tool install --global dotnet-ef >/dev/null 2>&1 || dotnet tool update --global dotnet-ef >/dev/null 2>&1 || true
export PATH="$PATH:$HOME/.dotnet/tools"

echo "[db] cho SQL Server (db:1433) san sang..."
for i in $(seq 1 45); do
  if (exec 3<>/dev/tcp/db/1433) 2>/dev/null; then exec 3>&- 3<&-; echo "  port mo"; break; fi
  sleep 2
done
sleep 8   # SQL Server can them vai giay de nhan auth sau khi port mo

cd src/MiniDMS.Web
if [ ! -d Migrations ]; then
  echo "[ef] migrations add InitialCreate..."
  dotnet ef migrations add InitialCreate || echo "  (add loi/da co)"
fi

echo "[ef] database update (retry toi 5 lan)..."
for i in $(seq 1 5); do
  if dotnet ef database update; then echo "  DB tao xong"; break; fi
  echo "  lan $i fail, cho 6s..."; sleep 6
done

echo ""
echo "=================================================="
echo " Chay app:  cd src/MiniDMS.Web && dotnet run"
echo " Mo tab PORTS -> 8080 (tu forward). Login xem SETUP.md."
echo " (Tuy chon) seed demo: apply database/002_seed.sql"
echo "=================================================="
