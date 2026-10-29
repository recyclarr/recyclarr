# ADR-003: Simplify OpenCode agent architecture to single primary agent

- **Scope:** `AGENTS.md`, `.opencode/`

## Context

An orchestrator agent that delegated to domain subagents (sync, config-schema, integration, cli-ux,
test, devops) added coordination cost without matching benefit in this single-stack .NET codebase.
In trials, a simple task (REC-55) took 15+ tool calls instead of about 8, and on a multi-domain task
(REC-26) writing delegation prompts took nearly as long as doing the work. Most subagent domain
knowledge duplicated `AGENTS.md` or was discoverable from code. Published guidance agrees: start
with one agent ([Anthropic][anthropic], [Cognition][cognition], [Microsoft][microsoft]).

## Decision

The repository defines no agents. One primary agent does the work, using `AGENTS.md` for durable
rules and `.opencode/skills/` for on-demand procedures (testing, changelog, decisions). MUST NOT add
domain-specialist subagents to the repository; put the knowledge in `AGENTS.md` or a skill.
Subagents for separate tool access or external lookup (for example `trash-guides`) live in the
user's global OpenCode configuration, not here.

## Rejected Options

- Orchestrator with domain subagents: coordination overhead exceeded the benefit (REC-55, REC-26).
- Domain subagents without an orchestrator: same duplicated knowledge, less context per agent.

## Consequences

- The primary agent carries broader context.
- A skill only helps when it loads; skill descriptions must name their triggers precisely.

## History

- 2026-01-17: Replace the orchestrator and domain subagents with one primary agent plus skills.
- 2026-10-16: Record matched to configuration: no agent definitions remain in the repository.

[anthropic]: https://www.anthropic.com/engineering/building-effective-agents
[cognition]: https://cognition.ai/blog/dont-build-multi-agents
[microsoft]: https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/the-future-of-ai-single-agent-or-multi-agent---how-should-i-choose/4257104
