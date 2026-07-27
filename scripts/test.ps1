$ErrorActionPreference = 'Stop'
$npmCommand = if ($IsWindows -or $env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }

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

Invoke-Checked 'dotnet' @('restore', 'MatterHarbor.sln', '--locked-mode')
Invoke-Checked 'dotnet' @('tool', 'restore')
Invoke-Checked 'dotnet' @('build', 'MatterHarbor.sln', '--no-restore')
Invoke-Checked 'dotnet' @('test', 'MatterHarbor.sln', '--no-build', '--filter', 'Category!=EndToEnd')
Invoke-Checked 'dotnet' @('format', 'MatterHarbor.sln', '--verify-no-changes', '--no-restore')
Invoke-Checked $npmCommand @('--prefix', 'src/MatterHarbor.Web', 'ci')
Invoke-Checked $npmCommand @('--prefix', 'src/MatterHarbor.Web', 'run', 'lint')
Invoke-Checked $npmCommand @('--prefix', 'src/MatterHarbor.Web', 'run', 'test')
Invoke-Checked $npmCommand @('--prefix', 'src/MatterHarbor.Web', 'run', 'build')
