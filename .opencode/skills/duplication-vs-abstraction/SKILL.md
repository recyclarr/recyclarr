---
name: duplication-vs-abstraction
description: >-
  Use when weighing whether to extract a shared abstraction, base class,
  interface, or generic helper versus keeping code duplicated; evaluating
  DRY tradeoffs across parallel Sonarr and Radarr implementations, Refit or
  other code-generated API clients, OpenAPI/gRPC/GraphQL DTOs, or
  anti-corruption layers over distinct external systems; reviewing a PR that
  introduces a new shared abstraction spanning independent bounded contexts;
  considering unwinding a wrong abstraction back into duplicated copies.
  Triggers on phrases like "should I abstract this", "extract a base class",
  "these look duplicated", "DRY this up", "unify Sonarr and Radarr", "refactor
  to share", "rule of three", "wrong abstraction". Do NOT use for mechanical
  refactors within a single type or for trivial helper extraction inside one
  module.
---

# Duplication vs Abstraction

Decision framework for when code that looks the same should stay duplicated versus being unified
behind a shared abstraction. Applies especially to code-generated types, multi-service adapters, and
parallel implementations with shared ancestry.

## Core Principles

**DRY is about knowledge, not syntax.** Hunt/Thomas (The Pragmatic Programmer) explicitly state the
principle targets knowledge duplication. Dave Thomas (2003 Artima interview): "Most people take DRY
to mean you shouldn't duplicate code. That's not the point." Two code paths that look identical but
represent different domain concepts are incidentally similar, not duplicated knowledge.

**Rule of Three.** Two instances do not warrant abstraction (Fowler/Beck, Refactoring). Three
instances provide enough data points to identify what varies and what stays the same. Abstract at
three, not two.

**Wrong abstraction > duplication.** Sandi Metz: "Duplication is far cheaper than the wrong
abstraction." The trajectory of a premature abstraction: extract, bend for new case, add
conditional, repeat until incomprehensible. Existing abstractions resist removal due to sunk-cost
inertia.

## Decision Checklist

Ask these questions in order. Stop at the first "no."

1. **Is this essential duplication?** Would fixing a bug in one copy always require the same fix in
   the other? If they could legitimately diverge, it is incidental duplication. Keep it duplicated.

2. **Do you have three or more instances?** Two instances give insufficient signal about the correct
   abstraction shape. Wait for the third.

3. **Is the abstraction's cost lower than the duplication's cost?** Count: generic type parameters,
   indirection layers, cognitive load for new readers, coupling surface. If the abstraction requires
   CRTP, 4+ type parameters, or forces callers to understand complex generic constraints, the cost
   likely exceeds the benefit.

4. **Are the types under your control?** Abstracting across code-generated types, external DTOs, or
   types from different bounded contexts creates fragile coupling to things you do not own. Prefer
   abstracting over your own domain types.

## Code-Generated Types

When multiple API clients produce near-identical types (OpenAPI codegen, gRPC, GraphQL):

- The structural similarity comes from shared ancestry or spec conventions, not shared domain
  knowledge
- Generated types will diverge on their own schedule, outside your control
- Layering interfaces on generated types (via partial classes or wrappers) creates a contract you
  must maintain as the generators evolve
- **Preferred approach**: if algorithmic logic is genuinely shared, extract it as a function
  operating on your own domain types, with separate per-service mappers that convert generated types
  to/from your domain

## Anti-Corruption Layers

DDD practitioners (Evans) prescribe one ACL per external bounded context. Sharing adapter
implementation across different external systems blurs the isolation boundary. Even when two
external APIs look identical today, they represent different domain agreements that evolve
independently.

## When Shared Abstraction IS Justified

- Three or more instances with genuinely identical semantics
- The shared algorithm is complex enough that maintaining divergent copies creates real bug risk
- The abstraction operates on types you control (your domain model), not external types
- The generic surface is simple (1-2 type parameters, no CRTP)

## Recovery: Unwinding a Wrong Abstraction

Per Metz and Abramov: inline first, re-extract later.

1. Copy the shared implementation back into each caller
2. Remove the abstraction (interfaces, generic utilities, base classes)
3. Let each copy evolve independently
4. If a correct abstraction emerges from three or more instances, extract then
