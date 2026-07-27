.PHONY: restore build test e2e format-check web-install web-build web-test web-lint verify infra-validate

restore:
	dotnet restore MatterHarbor.sln --locked-mode
	dotnet tool restore

build:
	dotnet build MatterHarbor.sln --no-restore

test:
	dotnet test MatterHarbor.sln --no-build --filter "Category!=EndToEnd"

e2e:
	pwsh ./scripts/test-e2e.ps1

format-check:
	dotnet format MatterHarbor.sln --verify-no-changes --no-restore

web-install:
	npm --prefix src/MatterHarbor.Web ci

web-build:
	npm --prefix src/MatterHarbor.Web run build

web-test:
	npm --prefix src/MatterHarbor.Web run test

web-lint:
	npm --prefix src/MatterHarbor.Web run lint

verify: restore build test format-check web-install web-lint web-test web-build

infra-validate:
	az bicep build --file infra/bicep/main.bicep
