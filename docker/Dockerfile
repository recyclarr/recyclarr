ARG DOTNET_VERSION=6.0
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-alpine

ARG VERSION=latest
ARG TARGETPLATFORM=linux/amd64
RUN set -ex; \
    \
    # The download path is a bit different when using latest release instead of tagged release
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
    # Download and extract the binary from the release
    wget --quiet -O /tmp/trash.zip "https://github.com/rcdailey/trash-updater/releases/$download_path/trash-$runtime.zip"; \
    apk add --no-cache unzip; \
    unzip -j /tmp/trash.zip -d /usr/local/bin trash; \
    chmod +x /usr/local/bin/trash; \
    \
    # Cleanup system
    apk del unzip; \
    rm -f /tmp/trash.zip; \
    rm -f /var/spool/cron/crontabs/root; \
    rm -rf /etc/periodic

# Runtime configuration options
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Define entrypoint (script that starts the cron job daemon or the CLI, depending on the arguments)
COPY --chmod=0755 ./entrypoint.sh /
ENTRYPOINT ["/entrypoint.sh"]
