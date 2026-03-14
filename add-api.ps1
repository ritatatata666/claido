param(
    [Parameter(Mandatory = $true)]
    [string]$ApiKey,
    [string]$EnvPath = ".env"
)

if (-not (Test-Path $EnvPath)) {
    "API_KEY=$ApiKey" | Set-Content $EnvPath
} else {
    $content = Get-Content $EnvPath
    if ($content -match '^API_KEY=') {
        $content = $content -replace '^API_KEY=.*$', "API_KEY=$ApiKey"
        $content | Set-Content $EnvPath
    } else {
        Add-Content $EnvPath "API_KEY=$ApiKey"
    }
}

Write-Host "Updated $EnvPath with API_KEY."