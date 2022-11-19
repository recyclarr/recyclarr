FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build

WORKDIR /build

ARG TARGETPLATFORM

RUN apk add unzip bash

COPY --chmod=544 ./scripts/build/*.sh .
COPY ./artifacts ./artifacts
RUN ./build.sh

#############################################################################
FROM alpine AS final

# Required by environment and/or dotnet
ENV PATH="${PATH}:/app/recyclarr" \
    RECYCLARR_APP_DATA=/config \
    CRON_SCHEDULE="@daily" \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/.net \
    # The GLOBALIZATION variable is so that we do not need libicu installed (saves us ~40MB).
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

RUN set -ex; \
    apk add --no-cache bash libstdc++ tzdata supercronic git; \
    mkdir -p "$DOTNET_BUNDLE_EXTRACT_BASE_DIR" && chmod 777 "$DOTNET_BUNDLE_EXTRACT_BASE_DIR"; \
    mkdir -p /config && chown 1000:1000 /config;

COPY --from=build /build/publish /app/recyclarr/
COPY --chmod=555 ./scripts/prod/*.sh /

USER 1000:1000
VOLUME /config

ENTRYPOINT ["/entrypoint.sh"]
