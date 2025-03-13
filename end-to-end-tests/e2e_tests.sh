#!/bin/bash 
# THIS FILE IS MEANT TO BE RUN USING THE run_me.sh FILE

# Check if server address is provided
if [[ "$#" -lt 1 ]]; then
  echo "Usage: $0 <server-address>"
  exit 1
fi

server1=$1

# Function to check the gRPC reflection endpoint
# shellcheck disable=SC2317
check_grpc_reflection() {
  response=$(grpcurl -plaintext $server1 list)
  echo "Response from gRPC reflection endpoint: $response"

  if [[ $response == *"TestServerGreeterOne"* && $response == *"TestServerGreeterTwo"* ]]; then
    echo "gRPC reflection endpoint verification successful."
    return 0
  elif [[ $response == *"TestServerGreeterOne"* ]]; then
    echo "Only TestServerGreeterOne found"
    return 1
  else
    echo "No greeter found in the response"
    return 1
  fi
}

# checks the routing works for both greeters
# shellcheck disable=SC2317
verify_routing_works(){
  echo "Checking routing works for both greeters..."
  response1=$(grpcurl -plaintext $server1 testGreet.TestServerGreeterOne.SayHelloOne)
  response2=$(grpcurl -plaintext $server1 testGreetTwo.TestServerGreeterTwo.SayHelloTwo)

  if [[ $response1 == *"Hello one"* && $response2 == *"Hello two"* ]]; then
    echo "Routing verification successful."
    return 0
  else
    echo "Routing verification failed."
    return 1
  fi
}

# Function to retry another function
retry() {
  local -r -i max_attempts="$1"
  local -r function_name="$2"
  local -i attempt=1

  while [[ $attempt -le $max_attempts ]]; do
    echo "Attempt $attempt of $max_attempts for $function_name..."
    $function_name
    result=$?
    if [[ $result -eq 0 ]]; then
      return 0
    # elif [[ $result -eq 1 ]]; then
      # return 1
    fi
    echo "Retrying $function_name in 10 seconds..."
    sleep 10
    attempt=$((attempt + 1))
  done

  echo "All attempts to run $function_name failed."
  return 1
}

# Retry logic for gRPC reflection
retry 4 check_grpc_reflection
result=$?
if [[ $result -eq 1 ]]; then
  exit 1
fi

# Retry logic for routing verification
retry 4 verify_routing_works
routing_result=$?
exit $routing_result