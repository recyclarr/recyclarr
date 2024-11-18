# syntax=docker/dockerfile:1.11-labs

# Following Microsoft's pattern shown here:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/dotnetapp/Dockerfile.alpine
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md

ARG DOTNET_VERSION=9.0

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build

ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers. Typically, packages change less often than code, so
# this cached layer is expected to be reused in the majority of cases.
#
# NOTE: --parents requires dockerfile version 1.7-labs since it isn't released yet. More info here:
# https://docs.docker.com/build/dockerfile/release-notes/#170
COPY --parents *.props src/*/*.csproj ./
RUN dotnet restore src/Recyclarr.Cli -a $TARGETARCH

# copy and publish app and libraries
COPY . .
RUN dotnet publish src/Recyclarr.Cli -a $TARGETARCH --no-restore -o /app

# Enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md
# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-alpine

LABEL name="recyclarr" \
  org.opencontainers.image.source="https://github.com/recyclarr/recyclarr" \
  org.opencontainers.image.url="https://recyclarr.dev" \
  org.opencontainers.image.licenses="MIT"

# Read below for the reasons why COMPlus_EnableDiagnostics is set:
# https://github.com/dotnet/docs/issues/10217
# https://github.com/dotnet/runtime/issues/96227
ENV PATH="${PATH}:/app/recyclarr" \
    RECYCLARR_APP_DATA=/config \
    CRON_SCHEDULE="@daily" \
    RECYCLARR_CREATE_CONFIG=false \
    COMPlus_EnableDiagnostics=0

RUN set -ex; \
    apk add --no-cache bash tzdata supercronic git tini; \
    mkdir -p /config && chown 1000:1000 /config;

COPY --from=build /app /app/recyclarr/
COPY --chmod=555 ./docker/scripts/prod/*.sh /

USER 1000:1000
VOLUME /config

ENTRYPOINT ["/sbin/tini", "--", "/entrypoint.sh"]
