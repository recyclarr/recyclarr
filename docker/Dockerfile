FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build

WORKDIR /build

ARG TARGETPLATFORM

RUN apk add unzip bash

COPY --chmod=544 ./scripts/build/*.sh .
COPY ./artifacts ./artifacts
RUN ./build.sh

#############################################################################
FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS final

# Required by environment and/or dotnet
ENV PATH="${PATH}:/app/recyclarr" \
    RECYCLARR_APP_DATA=/config \
    CRON_SCHEDULE="@daily" \
    RECYCLARR_CREATE_CONFIG=false

RUN set -ex; \
    apk add --no-cache bash tzdata supercronic git tini; \
    mkdir -p /config && chown 1000:1000 /config;

COPY --from=build /build/publish /app/recyclarr/
COPY --chmod=555 ./scripts/prod/*.sh /

USER 1000:1000
VOLUME /config

ENTRYPOINT ["/sbin/tini", "--", "/entrypoint.sh"]
