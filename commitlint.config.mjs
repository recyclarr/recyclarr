export default {
  extends: ["@commitlint/config-conventional"],
  ignores: [(commit) => /^wip/i.test(commit)],
  rules: {
    // Boundaries are defined in the commit-conventions skill
    "scope-enum": [
      2,
      "always",
      [
        "deps",
        "sync",
        "config",
        "yaml",
        "cli",
        "servarr",
        "server",
        "client",
        "docker",
        "agents",
      ],
    ],
  },
};
