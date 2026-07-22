$ErrorActionPreference = 'Stop'

docker compose up -d
dotnet restore MatterHarbor.sln
dotnet tool restore
Write-Host 'Dependencies are ready. Start these in separate terminals:'
Write-Host '  dotnet run --project src/MatterHarbor.Api'
Write-Host '  dotnet run --project src/MatterHarbor.Worker'
Write-Host '  npm --prefix src/MatterHarbor.Web run dev'
