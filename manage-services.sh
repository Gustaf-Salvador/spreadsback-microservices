#!/bin/bash

set -e

# Service Management Script for Spreadsback Microservices
# Usage: ./manage-services.sh [command] [service] [environment]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_REPO="Gustaf-Salvador/spreadsback-infra"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Available services
AVAILABLE_SERVICES=("user-service" "order-service" "product-service" "notification-service")

show_usage() {
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  list                              List all available services"
    echo "  status                            Show status of all services"
    echo "  build [service]                   Build specific service (or all if no service specified)"
    echo "  test [service]                    Test specific service (or all if no service specified)"
    echo "  deploy [service] [environment]    Deploy specific service to environment"
    echo "  deploy-all [environment]          Deploy all services to environment"
    echo "  validate                          Validate all services can build"
    echo ""
    echo "Services:"
    for service in "${AVAILABLE_SERVICES[@]}"; do
        echo "  - $service"
    done
    echo ""
    echo "Environments:"
    echo "  - nonprod (default)"
    echo "  - prod"
    echo ""
    echo "Examples:"
    echo "  $0 list"
    echo "  $0 build user-service"
    echo "  $0 deploy user-service nonprod"
    echo "  $0 deploy-all prod"
}

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Map service names to directories
get_service_directory() {
    local service=$1
    case "$service" in
        "user-service")
            echo "UserService"
            ;;
        "order-service")
            echo "orders"
            ;;
        "product-service")
            echo "products"
            ;;
        "notification-service")
            echo "notifications"
            ;;
        *)
            echo "$service"
            ;;
    esac
}

# Check if service exists
service_exists() {
    local service=$1
    local service_dir=$(get_service_directory "$service")
    [[ -d "$SCRIPT_DIR/$service_dir" ]]
}

# List all services
list_services() {
    log_info "Available microservices:"
    echo ""
    for service in "${AVAILABLE_SERVICES[@]}"; do
        local service_dir=$(get_service_directory "$service")
        if [[ -d "$SCRIPT_DIR/$service_dir" ]]; then
            echo -e "  ${GREEN}✓${NC} $service ($service_dir/)"
        else
            echo -e "  ${RED}✗${NC} $service ($service_dir/ - not found)"
        fi
    done
}

# Show status of services
show_status() {
    log_info "Service status:"
    echo ""
    for service in "${AVAILABLE_SERVICES[@]}"; do
        local service_dir=$(get_service_directory "$service")
        if [[ -d "$SCRIPT_DIR/$service_dir" ]]; then
            cd "$SCRIPT_DIR/$service_dir"
            
            local csproj_file=$(find . -name "*.csproj" -type f | head -1)
            if [[ -n "$csproj_file" ]]; then
                local last_modified=$(stat -f "%Sm" -t "%Y-%m-%d %H:%M" "$csproj_file" 2>/dev/null || stat -c "%y" "$csproj_file" 2>/dev/null | cut -d' ' -f1,2 | cut -d'.' -f1)
                echo -e "  ${GREEN}✓${NC} $service - Last modified: $last_modified"
            else
                echo -e "  ${YELLOW}?${NC} $service - No .csproj found"
            fi
            
            cd "$SCRIPT_DIR"
        else
            echo -e "  ${RED}✗${NC} $service - Directory not found"
        fi
    done
}

# Build a service
build_service() {
    local service=$1
    local service_dir=$(get_service_directory "$service")
    
    if ! service_exists "$service"; then
        log_error "Service '$service' not found in directory '$service_dir'"
        return 1
    fi
    
    log_info "Building $service..."
    cd "$SCRIPT_DIR/$service_dir"
    
    dotnet restore
    dotnet build --configuration Release --no-restore
    
    cd "$SCRIPT_DIR"
    log_success "Successfully built $service"
}

# Test a service
test_service() {
    local service=$1
    local service_dir=$(get_service_directory "$service")
    
    if ! service_exists "$service"; then
        log_error "Service '$service' not found in directory '$service_dir'"
        return 1
    fi
    
    log_info "Testing $service..."
    cd "$SCRIPT_DIR/$service_dir"
    
    # Check if tests exist
    if find . -name "*Test*.csproj" -o -name "*.Test.csproj" -o -name "*.Tests.csproj" | grep -q .; then
        dotnet test --configuration Release
        log_success "Tests passed for $service"
    else
        log_warning "No tests found for $service"
    fi
    
    cd "$SCRIPT_DIR"
}

# Deploy a service
deploy_service() {
    local service=$1
    local environment=${2:-nonprod}
    
    if ! service_exists "$service"; then
        log_error "Service '$service' not found"
        return 1
    fi
    
    log_info "Deploying $service to $environment environment..."
    
    # Validate build first
    build_service "$service"
    
    # Check if GitHub CLI is available
    if ! command -v gh &> /dev/null; then
        log_error "GitHub CLI (gh) is required for deployment. Please install it first."
        log_info "Install with: brew install gh"
        return 1
    fi
    
    # Check if authenticated
    if ! gh auth status &> /dev/null; then
        log_error "Not authenticated with GitHub. Run 'gh auth login' first."
        return 1
    fi
    
    # Trigger deployment via GitHub workflow
    log_info "Triggering infrastructure deployment workflow..."
    
    gh workflow run microservices-trigger-deployment.yml \
        --repo "Gustaf-Salvador/spreadsback-microservices" \
        -f environment="$environment" \
        -f services="$service" \
        -f force_all=false
    
    log_success "Deployment triggered for $service in $environment environment"
    log_info "Monitor progress at: https://github.com/Gustaf-Salvador/spreadsback-microservices/actions"
}

# Deploy all services
deploy_all() {
    local environment=${1:-nonprod}
    
    log_info "Deploying all services to $environment environment..."
    
    # Validate all services can build
    validate_all
    
    # Check if GitHub CLI is available
    if ! command -v gh &> /dev/null; then
        log_error "GitHub CLI (gh) is required for deployment. Please install it first."
        log_info "Install with: brew install gh"
        return 1
    fi
    
    # Trigger deployment via GitHub workflow
    log_info "Triggering infrastructure deployment workflow for all services..."
    
    gh workflow run microservices-trigger-deployment.yml \
        --repo "Gustaf-Salvador/spreadsback-microservices" \
        -f environment="$environment" \
        -f services="" \
        -f force_all=true
    
    log_success "Deployment triggered for all services in $environment environment"
    log_info "Monitor progress at: https://github.com/Gustaf-Salvador/spreadsback-microservices/actions"
}

# Validate all services
validate_all() {
    log_info "Validating all services..."
    local failed_services=()
    
    for service in "${AVAILABLE_SERVICES[@]}"; do
        if service_exists "$service"; then
            if ! build_service "$service"; then
                failed_services+=("$service")
            fi
        else
            log_warning "Service $service directory not found, skipping..."
        fi
    done
    
    if [[ ${#failed_services[@]} -eq 0 ]]; then
        log_success "All services validated successfully!"
    else
        log_error "Failed to validate services: ${failed_services[*]}"
        return 1
    fi
}

# Main script logic
main() {
    local command=${1:-}
    
    if [[ -z "$command" ]]; then
        show_usage
        exit 1
    fi
    
    case "$command" in
        "list")
            list_services
            ;;
        "status")
            show_status
            ;;
        "build")
            local service=$2
            if [[ -z "$service" ]]; then
                validate_all
            else
                build_service "$service"
            fi
            ;;
        "test")
            local service=$2
            if [[ -z "$service" ]]; then
                for svc in "${AVAILABLE_SERVICES[@]}"; do
                    if service_exists "$svc"; then
                        test_service "$svc"
                    fi
                done
            else
                test_service "$service"
            fi
            ;;
        "deploy")
            local service=$2
            local environment=$3
            if [[ -z "$service" ]]; then
                log_error "Service name required for deploy command"
                show_usage
                exit 1
            fi
            deploy_service "$service" "$environment"
            ;;
        "deploy-all")
            local environment=$2
            deploy_all "$environment"
            ;;
        "validate")
            validate_all
            ;;
        "help"|"-h"|"--help")
            show_usage
            ;;
        *)
            log_error "Unknown command: $command"
            show_usage
            exit 1
            ;;
    esac
}

# Check if script is being sourced or executed
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi