#!/usr/bin/env bash
# ============================================================
#  apikey-postgres.sh
#  Reads the current admin API key from the PostgreSQL database
#  and prints it to the console.
#
#  Usage:
#    ./apikey-postgres.sh           # print API key
#    ./apikey-postgres.sh --copy    # also copy to clipboard
#    ./apikey-postgres.sh --bench   # also update SnBenchmark appsettings.json
#    ./apikey-postgres.sh --help
# ============================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COPY=false
BENCH=false

# ── Parse args ───────────────────────────────────────────────
for arg in "$@"; do
  case "$arg" in
    --copy)  COPY=true ;;
    --bench) BENCH=true ;;
    --help|-h)
      echo "Usage: $0 [--copy] [--bench] [--help]"
      echo ""
      echo "Reads the admin API key from the PostgreSQL database."
      echo ""
      echo "Options:"
      echo "  --copy    Copy the key to clipboard (requires xclip or xsel)"
      echo "  --bench   Update tools/SnBenchmark/appsettings.json with the key"
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

# ── Read key ─────────────────────────────────────────────────
KEY=$(docker exec sensenet-postgres psql -U postgres -d sensenet-sndb -t -A -c \
  "SELECT \"Value\" FROM \"AccessTokens\" WHERE \"UserId\" = 1 AND \"ExpirationDate\" > NOW() ORDER BY \"CreationDate\" DESC LIMIT 1;" 2>/dev/null | tr -d '[:space:]')

if [ -z "$KEY" ]; then
  echo -e "${RED}✗  No API key found.${NC}"
  echo -e "${YELLOW}   Is the PostgreSQL stack running? Try: ./start-postgres.sh${NC}"
  exit 1
fi

echo ""
echo -e "${BOLD}══════════════════════════════════════════════════${NC}"
echo -e "${BOLD}  🔑  Admin API Key (PostgreSQL)${NC}"
echo -e "${BOLD}══════════════════════════════════════════════════${NC}"
echo -e "  ${GREEN}${KEY}${NC}"
echo -e "${BOLD}══════════════════════════════════════════════════${NC}"
echo ""

# ── Copy to clipboard ───────────────────────────────────────
if [ "$COPY" = true ]; then
  if command -v xclip &>/dev/null; then
    echo -n "$KEY" | xclip -selection clipboard
    echo -e "  ${GREEN}✓ Copied to clipboard${NC}"
  elif command -v xsel &>/dev/null; then
    echo -n "$KEY" | xsel --clipboard --input
    echo -e "  ${GREEN}✓ Copied to clipboard${NC}"
  elif command -v pbcopy &>/dev/null; then
    echo -n "$KEY" | pbcopy
    echo -e "  ${GREEN}✓ Copied to clipboard${NC}"
  else
    echo -e "  ${YELLOW}⚠ No clipboard tool found (install xclip or xsel)${NC}"
  fi
fi

# ── Update SnBenchmark ──────────────────────────────────────
if [ "$BENCH" = true ]; then
  BENCH_SETTINGS="$SCRIPT_DIR/../../tools/SnBenchmark/appsettings.json"
  if [ -f "$BENCH_SETTINGS" ]; then
    sed -i "s|\"ApiKey\": \".*\"|\"ApiKey\": \"$KEY\"|" "$BENCH_SETTINGS"
    echo -e "  ${GREEN}✓ Updated tools/SnBenchmark/appsettings.json${NC}"
  else
    echo -e "  ${YELLOW}⚠ SnBenchmark appsettings.json not found at $BENCH_SETTINGS${NC}"
  fi
fi

echo ""
