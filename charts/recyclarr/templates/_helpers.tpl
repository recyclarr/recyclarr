{{- define "recyclarr.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Names are truncated to 63 characters, the DNS label limit. A release name that already contains
the chart name is used as is.
*/}}
{{- define "recyclarr.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "recyclarr.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "recyclarr.labels" -}}
helm.sh/chart: {{ include "recyclarr.chart" . }}
{{ include "recyclarr.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "recyclarr.selectorLabels" -}}
app.kubernetes.io/name: {{ include "recyclarr.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "recyclarr.image" -}}
{{ printf "%s:%s" .Values.image.repository (.Values.image.tag | default .Chart.AppVersion) | quote }}
{{- end }}

{{- define "recyclarr.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "recyclarr.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Name of the Secret holding the API key: the user's existing Secret, or the one this chart creates.
Fails rendering unless exactly one of apiKey.existingSecret and apiKey.value is set; the server
refuses to start without a key.
*/}}
{{- define "recyclarr.apiKeySecretName" -}}
{{- $key := .Values.apiKey }}
{{- if and $key.existingSecret $key.value }}
{{- fail "Set only one of apiKey.existingSecret and apiKey.value" }}
{{- end }}
{{- if not (or $key.existingSecret $key.value) }}
{{- fail "Set apiKey.existingSecret or apiKey.value; the server requires an API key" }}
{{- end }}
{{- $key.existingSecret | default (printf "%s-api-key" (include "recyclarr.fullname" .)) }}
{{- end }}

{{/*
The user's env entries as YAML, or empty when there are none. Fails when an entry sets an API key
variable: the chart owns both, and the server refuses to start when both are set.
*/}}
{{- define "recyclarr.env" -}}
{{- range .Values.env }}
{{- if has .name (list "RECYCLARR_API_KEY" "RECYCLARR_API_KEY_FILE") }}
{{- fail (printf "env cannot set %s; the chart sets the API key from apiKey" .name) }}
{{- end }}
{{- end }}
{{- with .Values.env }}
{{- toYaml . }}
{{- end }}
{{- end }}

{{/*
Name of the ConfigMap for one file source (configs, includes, or settings), or empty when the
source is unset. Call with (dict "root" $ "source" "configs").
*/}}
{{- define "recyclarr.configMapName" -}}
{{- $values := index .root.Values .source }}
{{- $inline := ternary $values.content $values.files (eq .source "settings") }}
{{- if and $values.existingConfigMap $inline }}
{{- fail (printf "Set only one of %s.existingConfigMap and its inline files or content" .source) }}
{{- end }}
{{- if $values.existingConfigMap }}
{{- $values.existingConfigMap }}
{{- else if $inline }}
{{- printf "%s-%s" (include "recyclarr.fullname" .root) .source }}
{{- end }}
{{- end }}

{{/*
Name of the PersistentVolumeClaim for one volume (state or data), or empty when its persistence
is disabled. A StatefulSet names the claim it creates from a claim template
<template>-<statefulset>-<ordinal>. Call with (dict "root" $ "volume" "state").
*/}}
{{- define "recyclarr.claimName" -}}
{{- $values := index .root.Values.persistence .volume }}
{{- $fullname := include "recyclarr.fullname" .root }}
{{- if $values.enabled }}
{{- $created := ternary (printf "%s-%s-0" .volume $fullname) (printf "%s-%s" $fullname .volume) (eq .root.Values.controller.kind "StatefulSet") }}
{{- $values.existingClaim | default $created }}
{{- end }}
{{- end }}

{{/*
"true" when the chart creates the claim for one volume (state or data), or empty when the volume
uses an existingClaim or an emptyDir. Fails when dataSourceRef is set for a volume the chart does
not create a claim for, which would ignore it. Call with (dict "root" $ "volume" "state").
*/}}
{{- define "recyclarr.createsClaim" -}}
{{- $values := index .root.Values.persistence .volume }}
{{- $creates := and $values.enabled (not $values.existingClaim) }}
{{- if and $values.dataSourceRef (not $creates) }}
{{- fail (printf "persistence.%s.dataSourceRef needs a claim the chart creates: set enabled and unset existingClaim" .volume) }}
{{- end }}
{{- if $creates }}true{{ end }}
{{- end }}

{{/*
Spec of a claim the chart creates, shared by the standalone claims and the StatefulSet's claim
templates. Call with the volume's values.
*/}}
{{- define "recyclarr.claimSpec" -}}
accessModes:
  - {{ .accessMode }}
{{- with .storageClass }}
storageClassName: {{ . }}
{{- end }}
resources:
  requests:
    storage: {{ .size }}
{{- with .dataSourceRef }}
dataSourceRef:
  {{- toYaml . | nindent 2 }}
{{- end }}
{{- end }}

{{- define "recyclarr.statefulSet" -}}
{{- if eq .Values.controller.kind "StatefulSet" }}true{{ end }}
{{- end }}

{{/*
Volume source for one volume (state or data): its claim, or an emptyDir when persistence is
disabled. Fails when state and data name the same claim; both would mount its root, and the
server's lock on each directory would collide. Call with (dict "root" $ "volume" "state").
*/}}
{{- define "recyclarr.volumeSource" -}}
{{- $claim := include "recyclarr.claimName" . }}
{{- $other := include "recyclarr.claimName" (dict "root" .root "volume" (ternary "data" "state" (eq .volume "state"))) }}
{{- if and $claim (eq $claim $other) }}
{{- fail "persistence.state and persistence.data cannot use the same claim" }}
{{- end }}
{{- if $claim }}
persistentVolumeClaim:
  claimName: {{ $claim }}
{{- else }}
{{- $emptyDir := (index .root.Values.persistence .volume).emptyDir }}
{{- if or $emptyDir.sizeLimit $emptyDir.medium }}
emptyDir:
  {{- with $emptyDir.sizeLimit }}
  sizeLimit: {{ . }}
  {{- end }}
  {{- with $emptyDir.medium }}
  medium: {{ . }}
  {{- end }}
{{- else }}
emptyDir: {}
{{- end }}
{{- end }}
{{- end }}
