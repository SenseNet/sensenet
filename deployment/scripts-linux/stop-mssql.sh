#!/usr/bin/env bash
# ============================================================
#  stop-mssql.sh
#  Stops the sensenet MSSQL stack.
#
#  Usage:
#    ./stop-mssql.sh           # stop containers (keep data)
#    ./stop-mssql.sh --clean   # stop + remove volumes
#    ./stop-mssql.sh --help
# ============================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$SCRIPT_DIR/.."
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.mssql.yml"
CLEAN=false

# ── Parse args ───────────────────────────────────────────────
for arg in "$@"; do
  case "$arg" in
    --clean) CLEAN=true ;;
    --help|-h)
      echo "Usage: $0 [--clean] [--help]"
      echo ""
      echo "Stops the sensenet MSSQL stack."
      echo ""
      echo "Options:"
      echo "  --clean   Also remove Docker volumes (database data!)"
      echo "  --help    Show this help"
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

echo -e "${BOLD}"
echo "╔═══════════════════════════════════════════════╗"
echo "║   sensenet MSSQL — Stop                       ║"
echo "╚═══════════════════════════════════════════════╝"
echo -e "${NC}"

# ── Stop ─────────────────────────────────────────────────────
if [ "$CLEAN" = true ]; then
  step "Stopping containers and removing volumes..."
  docker compose -f "$COMPOSE_FILE" --profile reset down --volumes --remove-orphans 2>/dev/null || true
  ok "Containers stopped, volumes removed"
  warn "Database data has been permanently deleted!"
else
  step "Stopping containers (data preserved)..."
  docker compose -f "$COMPOSE_FILE" --profile reset down --remove-orphans 2>/dev/null || true
  ok "Containers stopped"
fi

echo ""
echo -e "  ${GREEN}Done.${NC} 👋"
echo ""
