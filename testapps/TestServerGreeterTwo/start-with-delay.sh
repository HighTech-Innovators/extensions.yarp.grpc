#!/bin/bash
echo "Waiting for 15 seconds before starting grpc-greeter-two..."
sleep 15
echo "Starting grpc-greeter-two..."
exec "$@"