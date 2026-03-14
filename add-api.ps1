$ApiKey = if ($args.Count -ge 1) { $args[0] } else { $null }
$EnvPath = if ($args.Count -ge 2) { $args[1] } else { ".env" }

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error "Usage: .\add-api.ps1 <API_KEY> [ENV_PATH]"
    exit 1
}

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