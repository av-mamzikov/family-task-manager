#!/bin/bash
# Start FamilyTaskManager System (Bot + Worker)
# Bash script for Linux/Mac

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
SKIP_BUILD=false
BOT_ONLY=false
WORKER_ONLY=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --bot-only)
            BOT_ONLY=true
            shift
            ;;
        --worker-only)
            WORKER_ONLY=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-build] [--bot-only] [--worker-only]"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}========================================"
echo -e "  FamilyTaskManager System Startup"
echo -e "========================================${NC}"
echo ""

# Get root directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
BOT_DIR="$ROOT_DIR/src/FamilyTaskManager.Bot"
WORKER_DIR="$ROOT_DIR/src/FamilyTaskManager.Worker"
INFRA_DIR="$ROOT_DIR/src/FamilyTaskManager.Infrastructure"

# PIDs
BOT_PID=""
WORKER_PID=""

# Check if .NET is installed
echo -e "${YELLOW}Checking .NET installation...${NC}"
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK version: $DOTNET_VERSION${NC}"
else
    echo -e "${RED}✗ .NET SDK not found. Please install .NET 9.0 or higher.${NC}"
    exit 1
fi

# Check PostgreSQL
echo ""
echo -e "${YELLOW}Checking PostgreSQL connection...${NC}"
if command -v psql &> /dev/null; then
    echo -e "${GREEN}✓ PostgreSQL client found${NC}"
else
    echo -e "${YELLOW}⚠ PostgreSQL client not found (optional)${NC}"
fi

# Check Bot token
if [ "$WORKER_ONLY" = false ]; then
    echo ""
    echo -e "${YELLOW}Checking Telegram Bot configuration...${NC}"
    cd "$BOT_DIR"
    if dotnet user-secrets list 2>/dev/null | grep -q "Bot:BotToken"; then
        echo -e "${GREEN}✓ Bot token configured${NC}"
    else
        echo -e "${RED}✗ Bot token not found${NC}"
        echo -e "${YELLOW}  Run: dotnet user-secrets set 'Bot:BotToken' 'YOUR_BOT_TOKEN'${NC}"
        exit 1
    fi
    cd "$ROOT_DIR"
fi

# Build projects
if [ "$SKIP_BUILD" = false ]; then
    echo ""
    echo -e "${YELLOW}Building projects...${NC}"
    cd "$ROOT_DIR"
    if dotnet build --configuration Release; then
        echo -e "${GREEN}✓ Build successful${NC}"
    else
        echo -e "${RED}✗ Build failed${NC}"
        exit 1
    fi
fi

# Apply migrations
echo ""
echo -e "${YELLOW}Applying database migrations...${NC}"
cd "$INFRA_DIR"
if dotnet ef database update --startup-project ../FamilyTaskManager.Web 2>/dev/null; then
    echo -e "${GREEN}✓ Database migrations applied${NC}"
else
    echo -e "${YELLOW}⚠ Could not apply migrations (database might be up to date)${NC}"
fi

# Cleanup function
cleanup() {
    echo ""
    echo ""
    echo -e "${CYAN}========================================"
    echo -e "  Stopping Services"
    echo -e "========================================${NC}"
    echo ""
    
    if [ ! -z "$BOT_PID" ]; then
        echo -e "${YELLOW}Stopping Bot (PID: $BOT_PID)...${NC}"
        kill $BOT_PID 2>/dev/null || true
        wait $BOT_PID 2>/dev/null || true
        echo -e "${GREEN}✓ Bot stopped${NC}"
    fi
    
    if [ ! -z "$WORKER_PID" ]; then
        echo -e "${YELLOW}Stopping Worker (PID: $WORKER_PID)...${NC}"
        kill $WORKER_PID 2>/dev/null || true
        wait $WORKER_PID 2>/dev/null || true
        echo -e "${GREEN}✓ Worker stopped${NC}"
    fi
    
    echo ""
    echo -e "${GREEN}System shutdown complete.${NC}"
    exit 0
}

# Set trap for cleanup
trap cleanup SIGINT SIGTERM

# Start services
echo ""
echo -e "${CYAN}========================================"
echo -e "  Starting Services"
echo -e "========================================${NC}"
echo ""

# Start Bot
if [ "$WORKER_ONLY" = false ]; then
    echo -e "${YELLOW}Starting Telegram Bot...${NC}"
    cd "$BOT_DIR"
    dotnet run --no-build --configuration Release > /tmp/familytaskmanager-bot.log 2>&1 &
    BOT_PID=$!
    echo -e "${GREEN}✓ Bot started (PID: $BOT_PID)${NC}"
    sleep 2
fi

# Start Worker
if [ "$BOT_ONLY" = false ]; then
    echo -e "${YELLOW}Starting Quartz Worker...${NC}"
    cd "$WORKER_DIR"
    dotnet run --no-build --configuration Release > /tmp/familytaskmanager-worker.log 2>&1 &
    WORKER_PID=$!
    echo -e "${GREEN}✓ Worker started (PID: $WORKER_PID)${NC}"
    sleep 2
fi

echo ""
echo -e "${CYAN}========================================"
echo -e "  System Running"
echo -e "========================================${NC}"
echo ""
echo -e "${GREEN}Services started successfully!${NC}"
echo ""
echo -e "${YELLOW}Active processes:${NC}"
if [ ! -z "$BOT_PID" ]; then
    echo -e "  ${CYAN}- Bot: PID $BOT_PID${NC}"
fi
if [ ! -z "$WORKER_PID" ]; then
    echo -e "  ${CYAN}- Worker: PID $WORKER_PID${NC}"
fi
echo ""
echo -e "${YELLOW}Log files:${NC}"
if [ "$WORKER_ONLY" = false ]; then
    echo -e "  ${CYAN}- Bot: /tmp/familytaskmanager-bot.log${NC}"
fi
if [ "$BOT_ONLY" = false ]; then
    echo -e "  ${CYAN}- Worker: /tmp/familytaskmanager-worker.log${NC}"
fi
echo ""
echo -e "${YELLOW}Commands:${NC}"
echo -e "  ${CYAN}View Bot logs:    tail -f /tmp/familytaskmanager-bot.log${NC}"
echo -e "  ${CYAN}View Worker logs: tail -f /tmp/familytaskmanager-worker.log${NC}"
echo -e "  ${CYAN}Stop system:      Press Ctrl+C${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop all services...${NC}"
echo ""

# Monitor processes
while true; do
    sleep 5
    
    # Check if Bot is still running
    if [ ! -z "$BOT_PID" ] && ! kill -0 $BOT_PID 2>/dev/null; then
        echo -e "${RED}✗ Bot process died unexpectedly!${NC}"
        echo -e "${YELLOW}Last 20 lines of Bot log:${NC}"
        tail -n 20 /tmp/familytaskmanager-bot.log
        cleanup
    fi
    
    # Check if Worker is still running
    if [ ! -z "$WORKER_PID" ] && ! kill -0 $WORKER_PID 2>/dev/null; then
        echo -e "${RED}✗ Worker process died unexpectedly!${NC}"
        echo -e "${YELLOW}Last 20 lines of Worker log:${NC}"
        tail -n 20 /tmp/familytaskmanager-worker.log
        cleanup
    fi
done
