FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . ./
RUN dotnet restore
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
COPY start-with-delay.sh /usr/local/bin/start-with-delay.sh
RUN chmod +x /usr/local/bin/start-with-delay.sh
EXPOSE 8082
ENTRYPOINT ["/usr/local/bin/start-with-delay.sh", "dotnet", "TestServerGreeterTwo.dll"]