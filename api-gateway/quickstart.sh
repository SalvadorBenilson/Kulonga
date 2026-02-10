#!/bin/bash
# Quick Start Script for API Gateway with SQL Server Docker Setup

set -e

echo "=========================================="
echo "API Gateway + SQL Server Docker Setup"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print colored output
print_step() {
    echo -e "${BLUE}→ $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if Docker is installed
print_step "Checking Docker installation..."
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi
print_success "Docker is installed"

# Check if Docker daemon is running
print_step "Checking Docker daemon..."
if ! docker ps &> /dev/null; then
    print_error "Docker daemon is not running. Please start Docker."
    exit 1
fi
print_success "Docker daemon is running"

# Check if docker-compose is available
print_step "Checking Docker Compose..."
if ! docker compose version &> /dev/null; then
    print_error "Docker Compose is not available."
    exit 1
fi
print_success "Docker Compose is available"

echo ""
print_step "Displaying available options:"
echo ""
echo "1) Start with production compose (recommended)"
echo "2) Start with debug compose"
echo "3) Build only (don't run)"
echo "4) Stop running containers"
echo "5) View logs"
echo "6) Clean up (remove containers and volumes)"
echo ""

read -p "Select option (1-6): " option

case $option in
    1)
        print_step "Building and starting with production compose..."
        docker compose down --remove-orphans 2>/dev/null || true
        docker compose up --build -d
        print_success "Services are starting..."
        echo ""
        print_step "Waiting for services to be healthy..."
        sleep 5
        
        if docker compose ps | grep -q "healthy"; then
            print_success "SQL Server is healthy"
        else
            print_warning "SQL Server is still starting, waiting..."
            sleep 10
        fi
        
        echo ""
        print_success "Setup complete!"
        echo ""
        echo "Services:"
        echo "  SQL Server: localhost:1433"
        echo "  API Gateway: http://localhost:5230"
        echo ""
        echo "Test the API Gateway:"
        echo "  curl http://localhost:5230/schools"
        echo ""
        echo "View logs:"
        echo "  docker compose logs -f"
        ;;
    2)
        print_step "Building and starting with debug compose..."
        docker compose -f compose.debug.yaml down --remove-orphans 2>/dev/null || true
        docker compose -f compose.debug.yaml up --build -d
        print_success "Debug services are starting..."
        echo ""
        echo "Services:"
        echo "  SQL Server (Debug): localhost:1433"
        echo "  API Gateway (Debug): http://localhost:5230"
        echo ""
        echo "View logs:"
        echo "  docker compose -f compose.debug.yaml logs -f"
        ;;
    3)
        print_step "Building images..."
        docker compose build --no-cache
        print_success "Build complete!"
        echo ""
        echo "To start the services, run:"
        echo "  docker compose up -d"
        ;;
    4)
        print_step "Stopping containers..."
        docker compose down
        print_success "Containers stopped"
        ;;
    5)
        print_step "Displaying logs..."
        docker compose logs -f --tail 100
        ;;
    6)
        print_warning "This will remove all containers, images, and volumes!"
        read -p "Are you sure? (yes/no): " confirm
        if [ "$confirm" = "yes" ]; then
            print_step "Cleaning up..."
            docker compose down --rmi all -v
            print_success "Cleanup complete"
        else
            print_warning "Cleanup cancelled"
        fi
        ;;
    *)
        print_error "Invalid option. Please select 1-6."
        exit 1
        ;;
esac

echo ""
