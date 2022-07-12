Recyclarr has an official Docker image hosted by the Github Container Registry (GHCR). The image
name is `ghcr.io/recyclarr/recyclarr`.

## Docker Compose Example

Before we get into the details of how to use the Docker image, I want to start with an example. I
personally hardly ever run `docker` commands directly. Instead, I use `docker compose` mainly
because the `docker-compose.yml` file is a fantastic way to keep configuration details in one place.
Thus, for the remainder of this page, all instruction and advice will be based on the example YAML
below. I highly recommend you set up your own `docker-compose.yml` this way.

Note that the below example should not be used verbatim. It's meant for example purposes only. Copy
& paste it but make the appropriate and necessary changes to it for your specific use case.

```yml
version: '3'

networks:
  recyclarr:
    name: recyclarr
    external: true

services:
  recyclarr:
    image: ghcr.io/recyclarr/recyclarr
    container_name: recyclarr
    init: true
    networks: [recyclarr]
    volumes:
      - ./config:/config
    environment:
      - TZ=America/Santiago
      - PUID=$DOCKER_UID
      - PGID=$DOCKER_GID
```

Here is a breakdown of the above YAML:

- `networks`<br>
  You are going to ultimately want Recyclarr to be able to connect to your Sonarr and Radarr
  instances. How you have Radarr and Sonarr hosted on your system will greatly impact how this part
  gets set up. In my case, I have a dedicated docker bridge network (in this example, named
  `recyclarr`) for those services. Naturally, that means I want Recyclarr to also run on that bridge
  network so it can access those services without going out and back in through my reverse proxy.
- `image`<br>
  The official Recyclarr image, hosted on Github.
- `container_name`<br>
  Optional, but I don't want the funky `prefix_recyclarr` name that Docker Compose uses for services
  by default.
- `init`<br>
  **Required**: This will ensure that the container can be stopped without terminating it when you
  run `docker compose down` or `docker compose stop`. Internally, this runs Recyclarr using
  [tini](https://github.com/krallin/tini). Please visit that repo to understand the benefits in
  detail, if you're interested.
- Stuff under `environment` is documented in the Environment section below.

## Tags

Tags for the docker image are broken down into the various components of the semantic version number
following the format of `X.Y.Z`, where:

- `X`: Represents a *major* release containing breaking changes.
- `Y`: Represents a *feature* release.
- `Z`: Represents a *bugfix* release.

The structure of the tags are described by the following table. Assume for example purposes we're
talking about `v2.1.2`. The table is sorted by *risk* in descending order. In other words, if you
value *stability* the most,  you want the bottom row. If you value being on *the bleeding edge*
(highest risk), you want the top row.

| Tag      | Description                                                             |
| -------- | ----------------------------------------------------------------------- |
| `latest` | Latest release, no matter what, including breaking changes              |
| `2`      | Latest *feature* and *bugfix* release; manual update for major releases |
| `2.1`    | Latest *bugfix* release; manual update if you want new features         |
| `2.1.2`  | Exact release; no automatic updates                                     |

## Configuration

### Volumes

- `/config`<br>
  This is the application data directory for Recyclarr. In this directory, files like
  `recyclarr.yml` and `settings.yml` exist, as well as `logs`, `cache`, and other directories.

### Environment

- `CRON_SCHEDULE` (Default: `@daily`)<br>
  Standard cron syntax for how often you want Recyclarr to run (see [Cron Mode](#cron-mode)).

- `TZ` (Default: `UTC`)<br>
  The time zone you want to use for Recyclarr's local time in the container.

- `PUID` (Default: `1000`)<br>
  The UID for the internal non-root user in the container. Match this to a UID on your host system
  if you're using a directory-mounted volume for `/config`.

- `PGID` (Default: `1000`)<br>
  The GID for the internal non-root user's group in the container. Match this to a GID on your host
  system if you're using a directory-mounted volume for `/config`.

## Modes

The docker container can operate in one of two different ways, which are documented below.

**TIP:** The first time you run Recyclarr in docker, it will automatically run the `create-config`
subcommand to create your `recyclarr.yml` file in the `/config` directory (in the container) if that
file does not exist yet.

### Manual Mode

In manual mode, the container starts up, runs a user-specified operation, and then exits. This is
semantically identical to running Recyclarr directly on your host machine, but without all of the
set up requirements.

The general syntax is:

```txt
docker compose run --rm recyclarr [subcommand] [options]
```

Where:

- `[subcommand]` is one of the supported Recyclarr subcommands, such as `sonarr` and `radarr`.
- `[options]` are any options supported by that subcommand (e.g. `--debug`, `--preview`).

Examples:

```sh
# Sync Sonarr with debug logs
docker compose run --rm recyclarr sonarr --debug

# Do a preview (dry run) sync for Radarr
docker compose run --rm recyclarr radarr --preview --debug
```

**TIP:** The `--rm` option ensures the container is deleted after it runs (without it, your list of
stopped containers will start to grow the more often you run it manually).

#### Warning about `docker exec`

I will not support any usage of `docker exec`, for now. It's far too error prone and can result in
mixed file permissions in Recyclarr's app data directory (the `/config` volume). Please use `docker
run --rm` instead (documented in the previous section).

When you run `docker exec` without the `--user` option, commands are executed as the internal root
user. If you absolutely insist on using this command, ensure you specify a user & group that matches
the `PUID` & `PGID` environment variables.

### Cron Mode

In this mode, no immediate action is performed. Rather, the container remains alive and continuously
runs both Sonarr and Radarr sync at whatever `CRON_SCHEDULE` you set (default is daily).

If either the Sonarr or Radarr sync operations fail, they will not prevent each other from
proceeding. In other words, if the order the sync happens is first Sonarr and then Radarr, if Sonarr
fails, the Radarr sync will still proceed after. From a linux shell perspective, it effectively runs
this command:

```sh
recyclarr sonarr; recyclarr radarr
```

To enter Cron Mode, you simply start the container in background mode:

```sh
docker compose up -d
```

This runs it without any subcommand or options, which will result in this mode being used.
