$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForHttp {
    param([Parameter(Mandatory)][string] $Uri)

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            Invoke-WebRequest -Uri $Uri -UseBasicParsing | Out-Null
            return
        }
        catch {
            if ($attempt -eq 30) {
                throw
            }
            Start-Sleep -Seconds 2
        }
    }
}

$configuration = 'Release'
$testProject = 'tests/MatterHarbor.EndToEndTests/MatterHarbor.EndToEndTests.csproj'
$playwrightScript = "tests/MatterHarbor.EndToEndTests/bin/$configuration/net10.0/playwright.ps1"

try {
    Invoke-Checked 'dotnet' @('restore', $testProject, '--locked-mode')
    Invoke-Checked 'dotnet' @('build', $testProject, '--configuration', $configuration, '--no-restore')
    & $playwrightScript install chromium
    if ($LASTEXITCODE -ne 0) {
        throw "Playwright browser installation failed with exit code $LASTEXITCODE."
    }
    Invoke-Checked 'docker' @('compose', '--file', 'compose.e2e.yaml', 'up', '--build', '--detach', '--wait')
    Wait-ForHttp 'http://127.0.0.1:5080/health/ready'
    Wait-ForHttp 'http://127.0.0.1:5173/'
    $env:MATTERHARBOR_E2E_BASE_URL = 'http://127.0.0.1:5173'
    $env:MATTERHARBOR_E2E_CONNECTION_STRING = 'Host=127.0.0.1;Port=5433;Database=matterharbor;Username=matterharbor;Password=matterharbor_e2e'
    Invoke-Checked 'dotnet' @('test', $testProject, '--configuration', $configuration, '--no-build')
}
finally {
    & docker compose --file compose.e2e.yaml logs
    & docker compose --file compose.e2e.yaml down --volumes
}
