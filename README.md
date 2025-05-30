## License

This project is licensed under the Common Public Attribution License Version 1.0 (CPAL-1.0).

You may obtain a copy of the License in the [LICENSE](./LICENSE) file.

For more details, you can also refer to the full license text at the [SPDX website](https://spdx.org/licenses/CPAL-1.0.html).

## Introduction

This project extends YARP to combine multiple gRPC reflection endpoints into a single one. It also filters the allowed calls and what's shown in the reflection endpoint based on the configuration regex.

## Configuration

- Hosts - list of hosts to combine the reflection endpoints from
- AllowedServiceRegex - regex to filter the services that are allowed to be shown in the reflection endpoint and proxied

## Usage

- add AddAutoGrpcReverseProxy() to your builder Program.cs
- add MapAutoGrpcReverseProxy() to your app in Program.cs

The configuration can be passed as an optional parameter to AddAutoGrpcReverseProxy or added in appsettings.json. Check end-to-end-tests\yarpgrpc.json for an example. 

## Testing

End to end tests
- cd ./end-to-end-tests
- ./run.sh

```yarpgrpc.json``` can be mounted at ```/config/yarpgrpc.json``` inside the container instead of using the environment variables in the compose.yml file.
