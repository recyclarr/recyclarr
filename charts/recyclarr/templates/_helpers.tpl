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
Name of the PersistentVolumeClaim for state, or empty when persistence is disabled.
*/}}
{{- define "recyclarr.stateClaimName" -}}
{{- if .Values.persistence.enabled }}
{{- .Values.persistence.existingClaim | default (printf "%s-state" (include "recyclarr.fullname" .)) }}
{{- end }}
{{- end }}
