# Reproducing the v0.1.0 build

MatterHarbor is not production-ready. Do not use it with real personal data.

Use the `v0.1.0` source tag in a clean checkout. The repository pins the .NET SDK, central NuGet versions and transitive lock graphs, Node dependencies, and the Node build image. These steps reproduce the release inputs and outputs; container layers are not promised to be byte-identical across registries or CPU architectures.

```bash
git clone https://github.com/ArinF1/MatterHarbor.git
cd MatterHarbor
git checkout --detach v0.1.0
git status --short
dotnet --version
node --version
docker version
```

`git status --short` must be empty and the .NET SDK must honor `global.json`.

## Build and test

```bash
dotnet restore MatterHarbor.sln --locked-mode
dotnet tool restore
dotnet build MatterHarbor.sln --configuration Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test MatterHarbor.sln --configuration Release --no-build --filter "Category!=EndToEnd"
dotnet format MatterHarbor.sln --verify-no-changes --no-restore
npm --prefix src/MatterHarbor.Web ci
npm --prefix src/MatterHarbor.Web run lint
npm --prefix src/MatterHarbor.Web run test
npm --prefix src/MatterHarbor.Web run build
pwsh ./scripts/test-e2e.ps1
```

## Security and packaging checks

```bash
dotnet list MatterHarbor.sln package --vulnerable --include-transitive
npm --prefix src/MatterHarbor.Web audit --audit-level=high
docker compose config --quiet
docker compose --file compose.e2e.yaml config --quiet
docker build --file src/MatterHarbor.Api/Dockerfile --tag matterharbor-api:v0.1.0 .
docker build --file src/MatterHarbor.Worker/Dockerfile --tag matterharbor-worker:v0.1.0 .
docker build --file src/MatterHarbor.Web/Dockerfile --tag matterharbor-web:v0.1.0 .
az bicep build --file infra/bicep/main.bicep
```

To record local artifact identities, use `docker image inspect` for image IDs and `sha256sum` (or PowerShell `Get-FileHash -Algorithm SHA256`) for files. No prebuilt binaries or production deployment are published for v0.1.0.
