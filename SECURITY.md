# Security Policy

## Supported Versions

Recyclarr follows semantic versioning and only the latest version receives security updates. We do
not provide security hotfixes for older releases since that does not align with Recyclarr's release
model. For this reason, it is extremely important to remain on the latest version.

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| Older   | :x:                |

## Dependency Security

Recyclarr regularly monitors and updates its dependencies to address security vulnerabilities. We
use automated tools to scan for known vulnerabilities and update affected packages as quickly as
possible. Security-related dependency updates are noted in the [changelog].

## Reporting a Vulnerability

Recyclarr takes security issues seriously. We appreciate your efforts to responsibly disclose your
findings.

Please report security vulnerabilities through GitHub's [Private vulnerability reporting][report]
feature. This allows us to assess and address the issue before it is publicly disclosed.

To report a vulnerability:

1. Go to <https://github.com/recyclarr/recyclarr/security/advisories/new>
2. Fill out the form with a detailed description of the vulnerability
3. Include steps to reproduce the issue if possible
4. Add any supporting materials (screenshots, PoC code, etc.)

We will acknowledge your report as soon as possible and keep you informed of our progress throughout
the remediation process.

[report]: https://github.com/recyclarr/recyclarr/security/advisories/new

## What to Expect

After submitting a report, it will be acknowledged and worked on as a high priority item. Once
fixed, generally we will hotfix the latest version and cut a new release. Details of the
vulnerability will appear in the [release notes][changelog].

## Security Best Practices

When using Recyclarr, consider these security recommendations:

- Always keep Recyclarr updated to the latest version
- Use API keys with the minimum required permissions
- For Docker installations, follow the principle of least privilege when setting up container
  permissions
- Review the logs regularly for any suspicious activity

[changelog]: https://github.com/recyclarr/recyclarr/blob/master/CHANGELOG.md
