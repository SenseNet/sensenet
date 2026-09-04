#!/usr/bin/env bash
# ============================================================
#  start-postgres.sh
#  Starts the sensenet PostgreSQL stack.
#
#  Usage:
#    ./start-postgres.sh              # start (rebuild if needed)
#    ./start-postgres.sh --no-build   # start without rebuild
#    ./start-postgres.sh --build      # force rebuild snapp image
#    ./start-postgres.sh --help
# ============================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$SCRIPT_DIR/.."
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.postgres.yml"
BUILD_ARG=""

# ── Parse args ───────────────────────────────────────────────
for arg in "$@"; do
  case "$arg" in
    --no-build) BUILD_ARG="--no-build" ;;
    --build)    BUILD_ARG="--build" ;;
    --help|-h)
      echo "Usage: $0 [--build] [--no-build] [--help]"
      echo ""
      echo "Starts the sensenet PostgreSQL stack."
      echo ""
      echo "Options:"
      echo "  --build      Force rebuild the snapp Docker image"
      echo "  --no-build   Skip building, use existing image"
      echo "  --help       Show this help"
      exit 0
      ;;
  esac
done

# ── Colors ───────────────────────────────────────────────────
GREEN='\033[0;32m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

step() { echo -e "\n${CYAN}${BOLD}▶ $1${NC}"; }
ok()   { echo -e "  ${GREEN}✓ $1${NC}"; }

echo -e "${BOLD}"
echo "╔═══════════════════════════════════════════════╗"
echo "║   sensenet PostgreSQL — Start                 ║"
echo "╚═══════════════════════════════════════════════╝"
echo -e "${NC}"

# ── Start ────────────────────────────────────────────────────
step "Starting the PostgreSQL stack..."
docker compose -f "$COMPOSE_FILE" up -d $BUILD_ARG
ok "All containers started"

# ── Wait for sensenet ────────────────────────────────────────
step "Waiting for sensenet to become ready..."
echo -e "  ${CYAN}(may take 1–3 minutes on fresh install)${NC}"

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
  echo -e "  ${CYAN}⚠ Timed out. Check logs: docker compose -f docker-compose.postgres.yml logs -f snapp${NC}"
fi

# ── Done ─────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}${BOLD}╔═══════════════════════════════════╗${NC}"
echo -e "${GREEN}${BOLD}║   ✅  Stack is running!           ║${NC}"
echo -e "${GREEN}${BOLD}╚═══════════════════════════════════╝${NC}"
echo ""
echo -e "  ${CYAN}Repository:${NC}  https://localhost:44362"
echo -e "  ${CYAN}SnAuth:${NC}      https://localhost:44311"
echo -e "  ${CYAN}pgAdmin:${NC}     http://localhost:5433"
echo -e "  ${CYAN}PostgreSQL:${NC}  localhost:5532"
echo ""
echo -e "  ${CYAN}API Key:${NC}     docker compose -f docker-compose.postgres.yml run --rm apikey"
echo -e "  ${CYAN}Logs:${NC}        docker compose -f docker-compose.postgres.yml logs -f snapp"
echo ""
