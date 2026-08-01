param(
    [string] $SourceVersion
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SourceVersion)) {
    $SourceVersion = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to determine the source commit.'
    }
}

if ($SourceVersion -notmatch '^[0-9A-Za-z._-]{1,100}$' -or $SourceVersion -in '.', '..') {
    throw 'SourceVersion must be a safe identifier using only letters, numbers, dots, underscores, and hyphens.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repositoryRoot 'artifacts/migrations'
$artifactDirectory = Join-Path $artifactRoot $SourceVersion
$resolvedLocksDirectory = Join-Path $artifactDirectory 'resolved-locks'
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    "matterharbor-migration-$([Guid]::NewGuid().ToString('N'))")
$runtimeIdentifier = 'linux-x64'
$bundleName = 'matterharbor-migrate'
$bundlePath = Join-Path $artifactDirectory $bundleName
$previousCi = $env:CI
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousLockedMode = $env:RestoreLockedMode
$lockFiles = @(
    'src/MatterHarbor.Domain/packages.lock.json',
    'src/MatterHarbor.Application/packages.lock.json',
    'src/MatterHarbor.Infrastructure/packages.lock.json',
    'src/MatterHarbor.Api/packages.lock.json'
)

function Copy-SourceDirectory {
    param(
        [string] $Source,
        [string] $Destination
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($item in Get-ChildItem -Force -LiteralPath $Source) {
        if ($item.PSIsContainer) {
            if ($item.Name -notin 'bin', 'obj') {
                Copy-SourceDirectory `
                    -Source $item.FullName `
                    -Destination (Join-Path $Destination $item.Name)
            }
        }
        else {
            Copy-Item -Force -LiteralPath $item.FullName -Destination $Destination
        }
    }
}

$locationPushed = $false
try {
    New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
    if (Test-Path -LiteralPath $artifactDirectory) {
        Remove-Item -Recurse -Force -LiteralPath $artifactDirectory
    }
    New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $resolvedLocksDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null

    foreach ($file in @(
        '.editorconfig',
        'Directory.Build.props',
        'Directory.Packages.props',
        'dotnet-tools.json',
        'global.json'
    )) {
        Copy-Item -Force -LiteralPath (Join-Path $repositoryRoot $file) `
            -Destination $stagingRoot
    }
    foreach ($project in @(
        'MatterHarbor.Domain',
        'MatterHarbor.Application',
        'MatterHarbor.Infrastructure',
        'MatterHarbor.Api'
    )) {
        Copy-SourceDirectory `
            -Source (Join-Path $repositoryRoot "src/$project") `
            -Destination (Join-Path $stagingRoot "src/$project")
    }

    Push-Location $stagingRoot
    $locationPushed = $true
    $env:CI = 'true'
    $env:ASPNETCORE_ENVIRONMENT = 'Production'

    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    $apiProject = Join-Path $stagingRoot 'src/MatterHarbor.Api/MatterHarbor.Api.csproj'
    $infrastructureProject = Join-Path $stagingRoot 'src/MatterHarbor.Infrastructure'
    & dotnet restore $apiProject --locked-mode
    if ($LASTEXITCODE -ne 0) {
        throw "Locked API restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build $apiProject `
        --configuration Release `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "API build failed with exit code $LASTEXITCODE."
    }

    # Prime the platform-specific graph, then require the actual bundle build to
    # use that exact graph. The enriched locks are shipped with the artifact.
    $env:RestoreLockedMode = 'false'
    & dotnet restore $apiProject `
        --runtime $runtimeIdentifier
    if ($LASTEXITCODE -ne 0) {
        throw "Runtime-specific restore failed with exit code $LASTEXITCODE."
    }

    $resolvedLockSnapshots = @{}
    foreach ($relativePath in $lockFiles) {
        $path = Join-Path $stagingRoot $relativePath
        $bytes = [System.IO.File]::ReadAllBytes($path)
        $resolvedLockSnapshots[$path] = $bytes
        $projectName = Split-Path -Leaf (Split-Path -Parent $relativePath)
        [System.IO.File]::WriteAllBytes(
            (Join-Path $resolvedLocksDirectory "$projectName.packages.lock.json"),
            $bytes)
    }

    $env:RestoreLockedMode = 'true'
    & dotnet restore $apiProject `
        --runtime $runtimeIdentifier `
        --locked-mode
    if ($LASTEXITCODE -ne 0) {
        throw "Locked runtime-specific restore failed with exit code $LASTEXITCODE."
    }

    & dotnet tool run dotnet-ef migrations bundle `
        --no-build `
        --force `
        --self-contained `
        --target-runtime $runtimeIdentifier `
        --output $bundlePath `
        --project $infrastructureProject `
        --startup-project (Split-Path -Parent $apiProject) `
        --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Migration bundle build failed with exit code $LASTEXITCODE."
    }

    foreach ($entry in $resolvedLockSnapshots.GetEnumerator()) {
        $current = [System.IO.File]::ReadAllBytes($entry.Key)
        if (
            [Convert]::ToBase64String($entry.Value) -ne
            [Convert]::ToBase64String($current)
        ) {
            throw "The locked bundle build changed dependency lock file '$($entry.Key)'."
        }
    }

    $SourceVersion | Set-Content -Encoding ascii -NoNewline -LiteralPath (
        Join-Path $artifactDirectory 'SOURCE_COMMIT')
    $toolManifest = Get-Content -Raw -LiteralPath (
        Join-Path $stagingRoot 'dotnet-tools.json') | ConvertFrom-Json
    $buildInfo = [ordered]@{
        sourceVersion = $SourceVersion
        dotnetSdkVersion = (& dotnet --version).Trim()
        dotnetEfVersion = $toolManifest.tools.'dotnet-ef'.version
        runtimeIdentifier = $runtimeIdentifier
        targetFramework = 'net10.0'
        selfContained = $true
    } | ConvertTo-Json
    [System.IO.File]::WriteAllText(
        (Join-Path $artifactDirectory 'BUILD_INFO.json'),
        "$buildInfo$([Environment]::NewLine)",
        (New-Object System.Text.UTF8Encoding))

    $hashInputs = @(
        [pscustomobject]@{ Path = $bundlePath; RelativePath = $bundleName }
        [pscustomobject]@{
            Path = Join-Path $artifactDirectory 'BUILD_INFO.json'
            RelativePath = 'BUILD_INFO.json'
        }
        [pscustomobject]@{
            Path = Join-Path $artifactDirectory 'SOURCE_COMMIT'
            RelativePath = 'SOURCE_COMMIT'
        }
    )
    $hashInputs += Get-ChildItem -File -LiteralPath $resolvedLocksDirectory |
        Sort-Object Name |
        ForEach-Object {
            [pscustomobject]@{
                Path = $_.FullName
                RelativePath = "resolved-locks/$($_.Name)"
            }
        }
    $hashLines = foreach ($inputFile in $hashInputs) {
        $hash = (
            Get-FileHash -Algorithm SHA256 -LiteralPath $inputFile.Path
        ).Hash.ToLowerInvariant()
        "$hash  $($inputFile.RelativePath)"
    }
    $hashLines | Set-Content -Encoding ascii -LiteralPath (
        Join-Path $artifactDirectory 'SHA256SUMS')

    Write-Output $artifactDirectory
}
finally {
    if ($locationPushed) {
        Pop-Location
    }
    $env:CI = $previousCi
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:RestoreLockedMode = $previousLockedMode
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -Recurse -Force -LiteralPath $stagingRoot
    }
}
