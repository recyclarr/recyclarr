# Recyclarr

Runs the [Recyclarr](https://recyclarr.dev) server, which syncs TRaSH Guides settings to Sonarr and
Radarr. The chart version is the Recyclarr version.

## Install

The server requires an API key. This example keeps it in a Secret you create:

```bash
kubectl create secret generic recyclarr-api-key \
  --from-literal=api-key="$(openssl rand -hex 32)"
helm install recyclarr oci://ghcr.io/recyclarr/charts/recyclarr \
  --set apiKey.existingSecret=recyclarr-api-key
```

Add your configuration files with `configs.files` or `configs.existingConfigMap`. Run
`helm show values oci://ghcr.io/recyclarr/charts/recyclarr` to see every value with its description.

## Documentation

The [Kubernetes install guide](https://recyclarr.dev/guide/installation/kubernetes/) covers
configuration files, secrets, storage, monitoring, and resource sizing.
