# httpyac Requests

- One smoke file per route namespace (`health`, `guide`, `instances`, `sync`) holding only the
  primary success path; every request there has a `# @name` so `-n` can run it alone. Error cases
  and secondary queries go in `<namespace>.checks.http`. Together they cover each endpoint's
  success case and every documented status code that a request can trigger deterministically.
- Files that need `service` or `instance` prompt with `$pick` from a global script only when the
  variable is undefined; a file-level `@var` would override `--var`. Prompts fail without a
  terminal, so non-interactive runs MUST pass `--var service=radarr` (or `sonarr`). `all` is valid
  only for `sync.http`.
- Assert with `??` or `test()` in `{{response}}` scripts. Body paths omit `$.` (`?? body items
  isArray`); httpyac 6.16.7 fails on `$.items`.
- Defaults live in `.httpyac.json` `$default`; override with `--var name=value`.
- Requests that change Sonarr/Radarr state MUST carry `# @disabled !destructive`. `--all` ignores
  `--tag`, so tags cannot exclude them.
- Exports from `# @loop` iterations do not reach later regions; share loop state via `$global`.
- Run against an existing server (root `AGENTS.md`). MUST wrap runs in `timeout`; polling loops can
  hang. Pass `--bail` so a failed or cancelled request stops the run:

```sh
timeout 120 httpyac send 'http/*.http' --all --bail --timeout 10000 --var service=radarr
timeout 60 httpyac send http/instances.http --all --bail --var service=radarr --var destructive=true
```
