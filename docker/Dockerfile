FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /build

ARG RELEASE_TAG=latest
ARG TARGETPLATFORM
ARG REPOSITORY=recyclarr/recyclarr

COPY --chmod=544 ./build.sh .

RUN apk add unzip
RUN ./build.sh

#############################################################################
FROM alpine AS final

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
    apk add --no-cache busybox-suid su-exec libstdc++ tzdata; \
    adduser --disabled-password --home "$HOME" recyclarr;

COPY --chown=recyclarr:recyclarr --chmod=544 --from=build /build/recyclarr /usr/local/bin
COPY --chown=recyclarr:recyclarr --chmod=544 entrypoint.sh /

ENTRYPOINT ["/entrypoint.sh"]
