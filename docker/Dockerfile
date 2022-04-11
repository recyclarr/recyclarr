# Globally scoped arguments (for FROM instructions)
#ARG BUILDPLATFORM=linux/amd64 <-- uncomment when building without Docker Buildx
ARG DOTNET_VERSION=6.0

# This build step just copies all binaries into the container and filters out the correct one
FROM --platform=$BUILDPLATFORM alpine:3.15 as get-trash-binary

# Copy all binaries into container
COPY ./publish /publish

ARG TARGETPLATFORM=linux/amd64
RUN set -ex; \
    \
    # Pick the correct trash binary according to target platform provided by Docker Buildx
    case "$TARGETPLATFORM" in \
		"linux/arm/v7") \
    		trash_binary="/publish/trash-linux-musl-arm/trash"  \
    		;; \
		"linux/arm64") \
    		trash_binary="/publish/trash-linux-musl-arm64/trash"  \
        	;; \
		"linux/amd64") \
    		trash_binary="/publish/trash-linux-musl-x64/trash"  \
    		;; \
		*) \
    		echo >&2 "ERROR: Unsupported target platform: $TARGETPLATFORM"; \
    		exit 1 \
    		;; \
	esac; \
    \
    # Confirm that the trash binary exists
    if [ ! -f "$trash_binary" ]; then  \
    	echo >&2 "ERROR: Binary not found: $trash_binary"; \
    	exit 1; \
    fi; \
    \
    # Move the trash binary into correct directory and set permissions
    mv "$trash_binary" /usr/local/bin/trash; \
    chmod 755 /usr/local/bin/trash


# This build step is contains the actual runtime/scripts
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-alpine

# Copy the trash binary from previous step
COPY --from=get-trash-binary /usr/local/bin/trash /usr/local/bin/trash

# Copy entrypoint script into container
COPY ./docker/entrypoint.sh /entrypoint.sh

RUN set -ex; \
    \
    # Set permissions of entrypoint script
    chmod 755 /entrypoint.sh; \
    \
    # Do some system cleanup (remove existing crontab file because it will be created by entrypoint script)
    rm -rf /var/spool/cron/crontabs/root; \
    rm -rf /etc/periodic

# Runtime configuration options (not sure if these actually affect anything but they wont't hurt)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Define entrypoint and reset command
ENTRYPOINT ["/entrypoint.sh"]
CMD []
