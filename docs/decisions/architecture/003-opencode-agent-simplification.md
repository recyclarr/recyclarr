# ADR-003: Simplify OpenCode agent architecture to single primary agent

- **Status:** accepted
- **Date:** 2026-01-17

## Context and Problem Statement

Implemented a multi-agent architecture inspired by Opencode-Workflows repo, with an orchestrator
that delegates to domain specialists (sync, config-schema, integration, cli-ux, test, devops).
Empirical testing revealed this pattern adds coordination overhead without proportional benefit for
Recyclarr's single-tech (.NET) codebase.

## Decision Drivers

- Orchestrator pattern designed for heterogeneous multi-tech stacks (React+Convex+Tailwind), not
  single-tech codebases
- Empirical testing: REC-55 (simple task) showed "orchestration added friction"; REC-26
  (multi-domain task) showed "moderate value" with prompt overhead approaching direct implementation
  time
- Industry research consensus: Anthropic ("start with simplest solution"), Cognition/Devin ("don't
  build multi-agents"), Microsoft ("before you try many agents, try just one")
- Domain knowledge in subagents was largely redundant with AGENTS.md or discoverable from code

## Considered Options

1. Keep orchestrator + domain subagents (status quo)
2. Keep subagents but remove orchestrator
3. Single primary agent with domain knowledge in AGENTS.md and skills for procedural knowledge

## Decision Outcome

Chosen option: "Single primary agent with domain knowledge in AGENTS.md", because empirical testing
showed orchestration overhead without proportional benefit, and most domain knowledge is either
already in AGENTS.md, discoverable from code exploration, or covered by skills.

### Consequences

- Good, because simpler architecture with less coordination overhead
- Good, because domain knowledge centralized in AGENTS.md for consistent context
- Good, because skills remain for procedural knowledge (testing, changelog, decisions)
- Good, because trash-guides subagent retained for external reference lookup (distinct use case)
- Good, because test and devops subagents retained for isolated domain work
- Bad, because less specialized agents may require broader context in primary agent
- Bad, because future complex multi-domain tasks may need to rebuild specialization

## Testing Conducted

### REC-55: Remove legacy cache field name aliases (simple, single-domain)

- 4 files, 56 lines deleted
- Orchestrator delegated to sync + test subagents across 5 task invocations
- Self-assessment: "Orchestration added friction for this task size"
- Metrics: 15+ tool calls vs ~8 direct, ~2000 tokens prompt overhead

### REC-26: Add language config YAML support (medium, multi-domain)

- 5 files, 126 lines added
- Used config-schema, sync, test subagents
- Self-assessment: "Moderate value" - prompt writing (~12 min) approached direct implementation (~15
  min)
- Subagent work was correct on first attempt

### Skill Usage

- Test subagent loaded `testing` skill before writing tests
- Sync and config-schema subagents did not load `csharp-coding` skill despite having permission
- Skills work but require explicit instructions to trigger consistently

## References

- [Anthropic: Building Effective
  Agents](https://www.anthropic.com/engineering/building-effective-agents)
- [Cognition: Don't Build Multi-Agents](https://cognition.ai/blog/dont-build-multi-agents)
- [Microsoft: Before You Try Many
  Agents](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/the-future-of-ai-single-agent-or-multi-agent---how-should-i-choose/4257104)
- [Opencode-Workflows repo](https://github.com/IgorWarzocha/Opencode-Workflows) - source of
  brain+orchestrator pattern
