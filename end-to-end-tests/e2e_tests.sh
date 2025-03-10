#!/bin/bash 
# THIS FILE IS MEANT TO BE RUN USING THE run_me.sh FILE

# Check if server address is provided
if [[ "$#" -lt 1 ]]; then
  echo "Usage: $0 <server-address>"
  exit 1
fi

server1=$1


# Call the gRPC reflection endpoint
response=$(grpcurl -plaintext $server1 list)
echo "Response from gRPC reflection endpoint: $response"

# Verify the response
if [[ $response == *"TestServerGreeterOne"* && $response == *"TestServerGreeterTwo"* ]]; then
  echo "gRPC reflection endpoint verification successful."
  exit 0
else
  echo "gRPC reflection endpoint verification failed."
  exit 1
fi