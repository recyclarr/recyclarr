ARG DOTNET_VERSION=6.0
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-alpine

# Copy the entrypoint script into the container
COPY ./entrypoint.sh /entrypoint.sh

ARG VERSION=latest
ARG TARGETPLATFORM
ARG REPOSITORY=rcdailey/trash-updater
RUN set -ex; \
    \
    # The download path is a bit different when using the latest release instead of a tagged release
    if [ "$VERSION" = "latest" ]; then \
        download_path="latest/download"; \
    else \
        download_path="download/$VERSION"; \
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
    # Download and extract the trash binary from the release
    wget --quiet -O /tmp/trash.zip "https://github.com/$REPOSITORY/releases/$download_path/trash-$runtime.zip"; \
    apk add --no-cache unzip; \
    unzip -j /tmp/trash.zip -d /usr/local/bin trash; \
    \
    # Set executable permissions
    chmod 755 /usr/local/bin/trash; \
    chmod 755 /entrypoint.sh; \
    \
    # Remove build dependencies, temporary files and the existing crontab file (as it will be overwritten anyways)
    apk del unzip; \
    rm -rf /tmp/trash.zip; \
    rm -rf /var/spool/cron/crontabs/root; \
    rm -rf /etc/periodic

# Define the entrypoint script as the entrypoint for the container
ENTRYPOINT ["/entrypoint.sh"]
