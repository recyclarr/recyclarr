FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /build

ARG RELEASE_TAG=latest
ARG TARGETPLATFORM
ARG REPOSITORY=recyclarr/recyclarr
ARG BUILD_FROM_BRANCH

COPY --chmod=544 ./scripts/build/*.sh .

RUN apk add unzip bash
RUN ./build.sh

#############################################################################
FROM alpine AS final

# Required by environment and/or dotnet
ENV RECYCLARR_APP_DATA=/config \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/.net \
    # Environment variables used by the entrypoint script. These may be overridden from `docker run`
    # as needed.
    CRON_SCHEDULE="@daily" \
    # The GLOBALIZATION variable is so that we do not need libicu installed (saves us ~40MB).
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

RUN set -e; \
    apk add --no-cache bash libstdc++ tzdata; \
    mkdir -p "$DOTNET_BUNDLE_EXTRACT_BASE_DIR" && chmod 777 "$DOTNET_BUNDLE_EXTRACT_BASE_DIR"

COPY --chmod=555 --from=build /build/recyclarr /usr/local/bin
COPY --chmod=555 ./scripts/prod/*.sh /

USER 1000:1000
VOLUME /config

ENTRYPOINT ["/entrypoint.sh"]
