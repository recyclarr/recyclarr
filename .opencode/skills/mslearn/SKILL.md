---
name: mslearn
description: >
  Use when searching official Microsoft documentation, fetching Microsoft
  Learn pages, or finding Microsoft/Azure code samples via the mslearn CLI
---

# Microsoft Learn CLI

CLI for searching and fetching official Microsoft documentation and code samples. Installed via
mise; no auth required.

## Commands

```bash
# Search documentation
mslearn search "azure functions timeout"

# Fetch a Learn page as markdown
mslearn fetch "https://learn.microsoft.com/azure/..."
mslearn fetch "https://learn.microsoft.com/azure/..." \
  --section "Configuration"
mslearn fetch "https://learn.microsoft.com/azure/..." --max-chars 3000

# Search code samples (optional language filter)
mslearn code-search "cosmos db change feed processor"
mslearn code-search "upload blob managed identity" --language csharp

# Diagnostics
mslearn doctor
```

Pass `--json` to `search` and `code-search` for structured output:

```bash
mslearn search "azure openai" --json | jq '.results[].title'
```

## Query Tips

Be specific; include version, task intent, and platform when relevant:

```bash
# Too broad
mslearn search "Azure Functions"

# Specific
mslearn search "Azure Functions Python v2 programming model"
mslearn search "Cosmos DB partition key design best practices"
```
