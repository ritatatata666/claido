param(
    [Parameter(Position = 0)]
    [string]$ApiKey
)

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "Usage: .\setup.ps1 <API_KEY>"
    exit 1
}

$repoRoot = $PSScriptRoot
$envPath = Join-Path $repoRoot ".env"
$frontendPath = Join-Path $repoRoot "frontend"

Set-Content -Path $envPath -Value @(
    "VITE_API_BASE_URL=http://localhost:5000"
    "API_KEY=$ApiKey"
)

if (-not (Test-Path $frontendPath)) {
    Write-Error "Frontend directory not found at $frontendPath"
    exit 1
}

Push-Location $frontendPath
try {
    npm install
}
finally {
    Pop-Location
}