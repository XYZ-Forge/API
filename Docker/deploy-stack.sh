#!/bin/bash

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

if ! command -v docker &>/dev/null; then
    echo "Error: Docker is not installed or not in the PATH."
    exit 1
fi

echo "Select the network interface for Docker Swarm."
select_interface

echo "Initializing Docker Swarm..."
initialize_swarm "$interface"

echo "Deploying Docker stack..."
deploy_stack

echo "Deployment completed successfully."
