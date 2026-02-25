---
description: Generate a documentation prompt for the wiki repo
---

Generate a self-contained prompt that will be used in a separate AI session to update the Recyclarr
wiki documentation. The prompt you generate is the ONLY output; no preamble, postamble, code blocks,
or quotation marks wrapping it.

The prompt must focus on WHAT changed and WHY, not HOW to write the documentation. Include:

- What features, fixes, or behavior changes occurred
- The intent and rationale behind each change
- How it affects users (new capabilities, changed behavior, removed functionality, new/changed YAML
  config properties, CLI changes)
- Any deprecations or breaking changes and their migration path
- Relevant YAML config examples if configuration changed (use real values, not placeholders)

The prompt must be completely self-contained. Embed all factual context directly in the prompt text.
Do not reference commits, PRs, or issues by identifier alone; always include the substantive
information from those sources inline. The receiving agent has no access to this repository, git
history, or any issue tracker.

<user-input>
"$ARGUMENTS"
</user-input>

If the <user-input> block above is empty, synthesize the prompt from our conversation context: what
we planned, implemented, and discussed in this session. You already have all the information needed.

If the <user-input> block contains text, it describes what to investigate. Perform a comprehensive
investigation to gather the necessary context:

- If commit SHAs or ranges are referenced, read them with git log/show and extract the meaningful
  changes
- If GitHub issues or PRs are referenced, fetch their content, comments, and linked items
- If Linear issues are referenced (REC-prefixed), read them including comments
- Follow cross-references between issues, PRs, and commits to build complete understanding
- Read any source code necessary to understand behavioral changes

After investigation, distill everything into the same self-contained prompt format described above.
