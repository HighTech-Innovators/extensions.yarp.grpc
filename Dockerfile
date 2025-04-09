# REFS
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS aspnet8
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime8
FROM ghcr.io/tarampampam/curl:8.12.1 AS curl
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS sdk8

#
#  Pre-restore tool image
#
FROM sdk8 AS prepare-restore-tool

RUN dotnet tool install --global dotnet-subset --version 0.3.2

##
#  Pre-restore image
#
FROM sdk8 AS prepare-restore-files

ENV PATH="${PATH}:/root/.dotnet/tools"
ENV CI=true
COPY --from=prepare-restore-tool /root/.dotnet/tools /root/.dotnet/tools/
WORKDIR /code
COPY src src
COPY tst tst
COPY testapps testapps
COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

RUN dotnet subset restore *.sln --root-directory . --output restore_subset/

#
# RESTORE
#
FROM sdk8 AS restore
WORKDIR /code

# Copy the subset of project/solution files needed for restore
COPY --from=prepare-restore-files /code/restore_subset .
ENV CI=true
# Run restore - it will use packages from /root/.nuget/packages if they exist
RUN dotnet restore

#
# BUILD
#
FROM restore AS build
COPY src src
COPY tst tst
COPY testapps testapps
RUN echo dotnet build -p:VERSION=0.0.0-local --no-restore -c Release
RUN dotnet build -p:VERSION=0.0.0-local --no-restore -c Release

#
# TEST
#
FROM build AS test
ARG DOCKER_HOST=
ENV DOCKER_HOST=${DOCKER_HOST}

ARG RunExternalProvidersTests=true

RUN dotnet test --no-build -c Release \
    $(if [ "$RunExternalProvidersTests" = "false" ]; then echo '--filter "FullyQualifiedName!~WithCustomTestContainersImplementation"'; fi)

###########################################################################################################
###########################################################################################################
###                                                                                                    ####
###                                               HOST BUILD AND PUBLISH                               ####  
###                                                                                                    ####
###########################################################################################################
###########################################################################################################

FROM sdk8 AS publish-host
WORKDIR /code

# Copy the subset of project/solution files needed for restore
COPY --from=prepare-restore-files /code/restore_subset .
ENV CI=true
ARG TARGETARCH
# Run restore - it will use packages from /root/.nuget/packages if they exist
RUN dotnet restore -a $TARGETARCH

COPY src src
COPY tst tst
COPY testapps testapps

ARG VERSION
RUN dotnet publish --no-restore \
 -c Release\
 -a $TARGETARCH \
 -p:VERSION=${VERSION}\
 -p:ServerGarbageCollection=false\
 -p:InvariantGlobalization=true\
 -p:EmitCompilerGeneratedFiles=true\
 -o /pub/Host\
  src/Host

###########################################################################################################
###########################################################################################################
###                                                                                                    ####
###                                               NUGET                                                ####
###                                                                                                    ####
###########################################################################################################
###########################################################################################################

#
# RELEASE BUILD (w/version)
#
FROM build AS releasebuild
ARG VERSION
RUN echo dotnet build -p:VERSION=${VERSION} --no-restore -c Release
RUN dotnet build\
      --no-restore\
      -c Release\
      -p:DebugType=embedded\
      -p:DebugSymbols=true\
      -p:Deterministic=true\
      -p:VERSION=${VERSION}\
      -p:ServerGarbageCollection=false\
      -p:InvariantGlobalization=true\
      -p:EmitCompilerGeneratedFiles=true\
      .
#
# PACKAGE
#
FROM releasebuild AS package
ARG VERSION
COPY icon.png .
COPY README.md .
RUN echo dotnet pack -p:VERSION=${VERSION} --no-build --no-restore -c Release -o /nuget/
RUN dotnet pack -p:VERSION=${VERSION} --no-build --no-restore -c Release -o /nuget/
RUN ls -l /nuget

#
# NUGET PUSH
#
FROM package AS nugetpush
ARG TARGET_NUGET
# Mount the secret file and read its content into the dotnet nuget push command
RUN --mount=type=secret,id=nuget_api_key \
    dotnet nuget push /nuget/*.nupkg -s ${TARGET_NUGET} -k "$(cat /run/secrets/nuget_api_key)"


###########################################################################################################
###########################################################################################################
###                                                                                                    ####
###                                               FINAL RUNTIME IMAGES                                 ####
###                                                                                                    ####
###########################################################################################################
###########################################################################################################


FROM aspnet8 AS final-host

# todo - add healthcheck
# # Docs: <https://docs.docker.com/engine/reference/builder/#healthcheck>
# HEALTHCHECK --interval=5s --timeout=2s --retries=2 --start-period=2s CMD [ \
#     "curl", "--fail", "http://127.0.0.1:8080/" \
# ]

COPY --from=publish-host /pub/Host/ /bin/yarpgrpc/

VOLUME /data

EXPOSE 8080/tcp

WORKDIR /bin/yarpgrpc
ENTRYPOINT ["dotnet", "Host.dll"]
