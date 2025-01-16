#!/bin/bash

check_root() {
    if [[ $EUID -ne 0 ]]; then
        echo "This script requires root privileges. Please run as root or use sudo."
        exec sudo "$0" "$@"
    fi
}

select_interface() {
    echo "Available network interfaces:"
    interfaces=$(ip -o link show | awk -F': ' '{print $2}')
    select interface in $interfaces; do
        if [[ -n "$interface" ]]; then
            echo "You selected: $interface"
            break
        else
            echo "Invalid selection. Please try again."
        fi
    done
}

initialize_swarm() {
    local interface=$1
    ip_address=$(ip -o -4 addr show "$interface" | awk '{print $4}' | cut -d'/' -f1)
    
    if [[ -z "$ip_address" ]]; then
        echo "Error: Could not determine IP address for interface $interface."
        exit 1
    fi

    echo "Initializing Docker Swarm on interface $interface (IP: $ip_address)..."
    docker swarm init --advertise-addr "$ip_address"
    if [[ $? -ne 0 ]]; then
        echo "Error: Docker Swarm initialization failed."
        exit 1
    fi
}

build_image() {
    local dockerfile="Dockerfile"
    local image_name="xyz-forge-api"
    
    if [[ ! -f $dockerfile ]]; then
        echo "Error: $dockerfile not found in the current directory."
        exit 1
    fi

    echo "Building Docker image..."
    docker build -t "$image_name" .
    if [[ $? -ne 0 ]]; then
        echo "Error: Docker image build failed."
        exit 1
    fi

    echo "Docker image built successfully."
}

deploy_stack() {
    local stack_file="docker-compose.yml"
    
    if [[ ! -f $stack_file ]]; then
        echo "Error: $stack_file not found in the current directory."
        exit 1
    fi

    echo "Deploying Docker stack..."
    docker stack deploy --compose-file "$stack_file" "${PWD##*/}-stack"
    if [[ $? -ne 0 ]]; then
        echo "Error: Stack deployment failed."
        exit 1
    fi

    echo "Docker stack deployed successfully."
}

stop_stack() {
    local stack_name="${PWD##*/}-stack"
    echo "Stopping and removing Docker stack..."
    docker stack rm "$stack_name"
    if [[ $? -ne 0 ]]; then
        echo "Error: Failed to remove Docker stack."
        exit 1
    fi

    echo "Docker stack removed successfully."
}

leave_swarm() {
    echo "Leaving Docker Swarm..."
    docker swarm leave --force
    if [[ $? -ne 0 ]]; then
        echo "Error: Failed to leave Docker Swarm."
        exit 1
    fi

    echo "Left Docker Swarm successfully."
}

start() {
    echo "Select the network interface for Docker Swarm."
    select_interface

    echo "Initializing Docker Swarm..."
    initialize_swarm "$interface"

    echo "Building Docker image..."
    build_image

    echo "Deploying Docker stack..."
    deploy_stack

    echo "Deployment completed successfully."
}

stop() {
    stop_stack
    leave_swarm
    echo "Stopped successfully."
}

check_root

if ! command -v docker &>/dev/null; then
    echo "Error: Docker is not installed or not in the PATH."
    exit 1
fi

case $1 in
    help)
        echo "Usage: $0 {start|stop}"
        exit 0
        ;;
    start)
        start
        ;;
    stop)
        stop
        ;;
    *)
        echo "Usage: $0 {start|stop}"
        exit 1
        ;;
esac
