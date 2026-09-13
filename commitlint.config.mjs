export default {
  extends: ['@commitlint/config-conventional'],
  ignores: [(commit) => /^wip/i.test(commit)],
};
