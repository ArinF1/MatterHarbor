## Summary

Describe the user-visible and architectural changes.

## Verification

- [ ] `dotnet format MatterHarbor.sln --verify-no-changes`
- [ ] `dotnet build MatterHarbor.sln`
- [ ] `dotnet test MatterHarbor.sln`
- [ ] `npm --prefix src/MatterHarbor.Web run lint`
- [ ] `npm --prefix src/MatterHarbor.Web run test`
- [ ] `npm --prefix src/MatterHarbor.Web run build`
- [ ] Documentation and migrations are updated when required

## Security and privacy

Describe tenant-isolation, authentication, logging, or personal-data implications.
