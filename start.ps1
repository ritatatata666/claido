$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frontendPath = Join-Path $repoRoot "frontend"
$backendPath = Join-Path $repoRoot "backend"

$shellCommand = if (Get-Command pwsh -ErrorAction SilentlyContinue) {
    "pwsh"
} else {
    "powershell"
}

Start-Process -FilePath $shellCommand -WorkingDirectory $frontendPath -ArgumentList @(
    "-NoExit",
    "-Command",
    "npm install; if (`$LASTEXITCODE -eq 0) { npm run dev }"
)

Start-Process -FilePath $shellCommand -WorkingDirectory $backendPath -ArgumentList @(
    "-NoExit",
    "-Command",
    "dotnet run"
)
