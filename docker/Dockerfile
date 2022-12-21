FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine as base

FROM base AS base-arm
ENV RUNTIME=linux-musl-arm

FROM base AS base-arm64
ENV RUNTIME=linux-musl-arm64

FROM base AS base-amd64
ENV RUNTIME=linux-musl-x64

FROM base-$TARGETARCH AS final

# Required by environment and/or dotnet
ENV PATH="${PATH}:/app/recyclarr" \
    RECYCLARR_APP_DATA=/config \
    CRON_SCHEDULE="@daily" \
    RECYCLARR_CREATE_CONFIG=false

RUN set -ex; \
    apk add --no-cache bash tzdata supercronic git tini; \
    mkdir -p /config && chown 1000:1000 /config;

COPY ./artifacts/$RUNTIME /app/recyclarr/
COPY --chmod=555 ./scripts/prod/*.sh /

USER 1000:1000
VOLUME /config

ENTRYPOINT ["/sbin/tini", "--", "/entrypoint.sh"]
