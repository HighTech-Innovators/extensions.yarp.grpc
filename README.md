## License

  

This project is licensed under the Common Public Attribution License Version 1.0 (CPAL-1.0).

  

You may obtain a copy of the License in the [LICENSE](./LICENSE) file.

  

For more details, you can also refer to the full license text at the [SPDX website](https://spdx.org/licenses/CPAL-1.0.html).

  

## Introduction

  

This project extends YARP to combine multiple gRPC reflection endpoints into a single one. It also filters the allowed calls and what's shown in the reflection endpoint based on the configuration regex.

  

## Configuration

- Hosts - list of hosts to combine the reflection endpoints from

- AllowedServiceRegex - regex to filter the services that are allowed to be shown in the reflection endpoint and proxied
