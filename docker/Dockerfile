FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /build

ARG RELEASE_TAG=latest
ARG TARGETPLATFORM
ARG REPOSITORY=recyclarr/recyclarr

RUN apk add unzip

RUN set -ex; \
    \
    # The download path is a bit different when using the latest release instead of a specific
    # release
    if [ "$RELEASE_TAG" = "latest" ]; then \
        download_path="latest/download"; \
    else \
        download_path="download/$RELEASE_TAG"; \
    fi; \
    \
    # Determine the runtime from the target platform provided by Docker Buildx
    case "$TARGETPLATFORM" in \
        "linux/arm/v7") runtime="linux-musl-arm" ;; \
        "linux/arm64") runtime="linux-musl-arm64" ;; \
        "linux/amd64") runtime="linux-musl-x64" ;; \
        *) echo >&2 "ERROR: Unsupported target platform: $TARGETPLATFORM"; exit 1 ;; \
    esac; \
    \
    # Download and extract the recyclarr binary from the release
    wget --quiet -O recyclarr.zip "https://github.com/$REPOSITORY/releases/$download_path/recyclarr-$runtime.zip"; \
    unzip recyclarr.zip;

#############################################################################
FROM alpine

# Required by environment and/or dotnet
ENV HOME=/config \
    DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/.net \
    # Environment variables used by the entrypoint script. These may be overridden from `docker run`
    # as needed.
    CRON_SCHEDULE="@daily" \
    # The GLOBALIZATION variable is so that we do not need libicu installed (saves us ~40MB).
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

VOLUME /config

RUN set -ex; \
    apk add --no-cache busybox-suid su-exec libstdc++; \
    adduser --disabled-password --home "$HOME" recyclarr;

COPY --chown=recyclarr:recyclarr --chmod=544 --from=build /build/recyclarr /usr/local/bin
COPY --chown=recyclarr:recyclarr --chmod=544 entrypoint.sh /

ENTRYPOINT ["/entrypoint.sh"]
