# Following Microsoft's pattern shown here:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/dotnetapp/Dockerfile.alpine
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers. Typically, packages change less often than code, so
# this cached layer is expected to be reused in the majority of cases.
COPY --link --parents *.props src/*/*.csproj ./
RUN dotnet restore src/Recyclarr.Cli -a $TARGETARCH \
 && dotnet restore src/Recyclarr.Server -a $TARGETARCH

# copy and publish app and libraries
COPY --link . .
RUN dotnet publish src/Recyclarr.Cli -a $TARGETARCH --no-restore -o /app \
 && dotnet publish src/Recyclarr.Server -a $TARGETARCH --no-restore -o /app \
    -p:OpenApiGenerateDocuments=false

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

LABEL name="recyclarr" \
  org.opencontainers.image.source="https://github.com/recyclarr/recyclarr" \
  org.opencontainers.image.url="https://recyclarr.dev" \
  org.opencontainers.image.licenses="MIT"

# Read below for the reasons why COMPlus_EnableDiagnostics is set:
# https://github.com/dotnet/docs/issues/10217
# https://github.com/dotnet/runtime/issues/96227
#
# The server always listens on all interfaces at 7982; users remap the port with Docker. An
# explicit URL makes the server ignore server.bind_address and server.port from settings.yml.
# ASPNETCORE_HTTP_PORTS is cleared so the base image's 8080 default does not compete with it.
# RECYCLARR_SERVER_URL makes `docker exec <container> recyclarr ...` use this server instead of
# starting a private one against the same /config.
ENV PATH="${PATH}:/app/recyclarr" \
    RECYCLARR_CONFIG_DIR=/config \
    ASPNETCORE_URLS=http://0.0.0.0:7982 \
    ASPNETCORE_HTTP_PORTS= \
    RECYCLARR_SERVER_URL=http://127.0.0.1:7982 \
    COMPlus_EnableDiagnostics=0

RUN set -ex; \
    apk add --no-cache bash tzdata git tini; \
    mkdir -p /config /data && chown 1000:1000 /config /data;

COPY --link --from=build /app /app/recyclarr/

USER 1000:1000
VOLUME /config
EXPOSE 7982

# 127.0.0.1, not localhost: busybox wget tries ::1 first, and the server listens on IPv4 only.
HEALTHCHECK CMD wget -q --spider http://127.0.0.1:7982/health || exit 1

ENTRYPOINT ["/sbin/tini", "--"]
CMD ["recyclarr-server"]
