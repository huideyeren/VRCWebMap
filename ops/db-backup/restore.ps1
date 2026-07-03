param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Selector
)

$ErrorActionPreference = "Stop"
$ExitUsage = 64
$ExitRestart = 71
$RepositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "../.."))

Set-Location $RepositoryRoot

if (-not (Test-Path ".env.backup" -PathType Leaf)) {
    Write-Error "restore: .env.backup was not found in $RepositoryRoot"
    exit $ExitUsage
}

$ComposeArguments = @(
    "compose",
    "--env-file", ".env.backup",
    "--profile", "backup"
)

# backend停止前にlatestを不変のobject keyへ解決します。
$ResolvedOutput = & docker @ComposeArguments run --rm db-backup resolve $Selector
$ResolveExitCode = $LASTEXITCODE
if ($ResolveExitCode -ne 0) {
    exit $ResolveExitCode
}

$ResolvedKey = [string]($ResolvedOutput | Select-Object -Last 1)
$ResolvedKey = $ResolvedKey.Trim()
if ([string]::IsNullOrWhiteSpace($ResolvedKey)) {
    Write-Error "restore: backup key could not be resolved"
    exit $ExitUsage
}

Write-Host "Restore target:"
& docker @ComposeArguments run --rm db-backup describe $ResolvedKey
$DescribeExitCode = $LASTEXITCODE
if ($DescribeExitCode -ne 0) {
    exit $DescribeExitCode
}

$Confirmation = Read-Host "Type vrcwebmap to restore $ResolvedKey"
if ($Confirmation -cne "vrcwebmap") {
    Write-Error "restore: confirmation did not match; no services were stopped"
    exit $ExitUsage
}

$RunningServices = @(
    & docker @ComposeArguments ps --status running --services backend
)
$PsExitCode = $LASTEXITCODE
if ($PsExitCode -ne 0) {
    exit $ExitUsage
}

$BackendWasRunning = $RunningServices -contains "backend"
if ($BackendWasRunning) {
    & docker @ComposeArguments stop backend
    if ($LASTEXITCODE -ne 0) {
        exit $ExitUsage
    }
}

& docker @ComposeArguments run --rm db-backup restore $ResolvedKey
$RestoreExitCode = $LASTEXITCODE
if ($RestoreExitCode -ne 0) {
    Write-Error "restore: failed; backend remains stopped"
    exit $RestoreExitCode
}

if ($BackendWasRunning) {
    & docker @ComposeArguments up --detach backend
    if ($LASTEXITCODE -ne 0) {
        Write-Error "restore: database restore succeeded, but backend restart failed"
        exit $ExitRestart
    }
}

Write-Host "restore: completed successfully"
exit 0
