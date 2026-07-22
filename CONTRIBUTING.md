# Contributing

Thank you for improving MatterHarbor. Start with an issue for behavior or architecture changes. Small fixes may go directly to a pull request.

## Workflow

1. Create a focused branch using a conventional prefix such as `feat/`, `fix/`, or `docs/`.
2. Preserve module boundaries and organization scoping.
3. Add or update tests at the lowest useful layer plus integration coverage for persistence behavior.
4. Run `./scripts/test.ps1` or the equivalent README commands.
5. Update documentation that describes status, APIs, architecture, security, or operations.
6. Use Conventional Commits, for example `feat: add case filtering`.

Never commit secrets, `.env` files, generated build output, or real personal/case data. Security issues follow [SECURITY.md](SECURITY.md), not public issues.
