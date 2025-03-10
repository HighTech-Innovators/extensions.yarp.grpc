#!/bin/bash

# Call the gRPC reflection endpoint
response=$(grpcurl -plaintext localhost:8080 list)
echo "Response from gRPC reflection endpoint: $response"

# Verify the response
if [[ $response == *"TestServerGreeterOne"* && $response == *"TestServerGreeterTwo"* ]]; then
  echo "gRPC reflection endpoint verification successful."
  exit 0
else
  echo "gRPC reflection endpoint verification failed."
  exit 1
fi