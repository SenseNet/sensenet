#!/usr/bin/env bash
# ============================================================
#  reset-mssql.sh
#  Stops, resets, and restarts the sensenet MSSQL stack.
#
#  Usage:
#    ./reset-mssql.sh              # full reset + rebuild
#    ./reset-mssql.sh --no-build   # full reset, no rebuild
#    ./reset-mssql.sh --help
# ============================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$SCRIPT_DIR/.."
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.mssql.yml"
APP_DATA="$DEPLOY_DIR/App_Data"
REBUILD=true

# ── Parse args ───────────────────────────────────────────────
for arg in "$@"; do
  case "$arg" in
    --no-build) REBUILD=false ;;
    --help|-h)
      echo "Usage: $0 [--no-build] [--help]"
      echo ""
      echo "Stops all containers, drops the MSSQL database,"
      echo "clears the Lucene index, rebuilds the snapp image,"
      echo "and starts everything fresh."
      echo ""
      echo "Options:"
      echo "  --no-build   Skip rebuilding the snapp Docker image"
      echo "  --help       Show this help"
      exit 0
      ;;
  esac
done

# ── Colors ───────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

step() { echo -e "\n${CYAN}${BOLD}▶ $1${NC}"; }
ok()   { echo -e "  ${GREEN}✓ $1${NC}"; }
warn() { echo -e "  ${YELLOW}⚠ $1${NC}"; }
fail() { echo -e "  ${RED}✗ $1${NC}"; exit 1; }

echo -e "${BOLD}"
echo "╔═══════════════════════════════════════════════╗"
echo "║      sensenet MSSQL — Full Reset & Restart    ║"
echo "╚═══════════════════════════════════════════════╝"
echo -e "${NC}"

# ── 1. Stop all containers ──────────────────────────────────
step "Stopping all containers..."
docker compose -f "$COMPOSE_FILE" --profile reset down --remove-orphans 2>/dev/null || true
ok "Containers stopped"

# ── 2. Drop the database via reset profile ──────────────────
step "Starting MSSQL and running DB reset..."
docker compose -f "$COMPOSE_FILE" --profile reset up db-reset --abort-on-container-exit 2>&1
RESET_EXIT=$?
if [ $RESET_EXIT -ne 0 ]; then
  warn "Reset profile exited with code $RESET_EXIT (may be OK if DB didn't exist)"
fi

# Also make sure index is cleared on the host side
if [ -d "$APP_DATA/LocalIndex" ]; then
  rm -rf "$APP_DATA/LocalIndex"/*
  ok "Lucene index cleared (host)"
else
  warn "No LocalIndex directory found at $APP_DATA/LocalIndex"
fi

# ── 3. Stop the reset containers ────────────────────────────
step "Stopping reset containers..."
docker compose -f "$COMPOSE_FILE" --profile reset down --remove-orphans 2>/dev/null || true
ok "Reset containers stopped"

# ── 4. Rebuild snapp image ──────────────────────────────────
if [ "$REBUILD" = true ]; then
  step "Rebuilding snapp image..."
  docker compose -f "$COMPOSE_FILE" build snapp
  ok "Image rebuilt"
else
  warn "Skipping rebuild (--no-build)"
fi

# ── 5. Start the full stack ─────────────────────────────────
step "Starting the full stack..."
docker compose -f "$COMPOSE_FILE" up -d
ok "All containers started"

# ── 6. Wait for sensenet to become ready ────────────────────
step "Waiting for sensenet to become ready..."
echo -e "  ${CYAN}(this may take 1–3 minutes for fresh install)${NC}"

MAX_WAIT=180
ELAPSED=0
while [ $ELAPSED -lt $MAX_WAIT ]; do
  HTTP_CODE=$(curl -sk -o /dev/null -w "%{http_code}" https://localhost:44362/odata.svc/Root 2>/dev/null || echo "000")
  if [ "$HTTP_CODE" = "200" ]; then
    ok "sensenet is ready! (HTTP 200 after ${ELAPSED}s)"
    break
  fi
  printf "\r  ⏳ Waiting... %3ds / %ds  (last HTTP: %s)" "$ELAPSED" "$MAX_WAIT" "$HTTP_CODE"
  sleep 3
  ELAPSED=$((ELAPSED + 3))
done

if [ $ELAPSED -ge $MAX_WAIT ]; then
  echo ""
  warn "Timed out after ${MAX_WAIT}s. Check logs: docker compose -f docker-compose.mssql.yml logs -f snapp"
fi

# ── Done ────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}${BOLD}╔═══════════════════════════════════╗${NC}"
echo -e "${GREEN}${BOLD}║   ✅  Reset complete!             ║${NC}"
echo -e "${GREEN}${BOLD}╚═══════════════════════════════════╝${NC}"
echo ""
echo -e "  ${CYAN}Repository:${NC}  https://localhost:44362"
echo -e "  ${CYAN}SnAuth:${NC}      https://localhost:44311"
echo -e "  ${CYAN}MSSQL:${NC}       localhost:9999"
echo ""
echo -e "  ${CYAN}Logs:${NC}        docker compose -f docker-compose.mssql.yml logs -f snapp"
echo ""
