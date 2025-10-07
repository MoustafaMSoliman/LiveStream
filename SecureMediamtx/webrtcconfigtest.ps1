# Check global configuration using v3 API
Write-Host "Checking MediaMTX configuration..."
try {
    $config = Invoke-WebRequest -Uri "http://localhost:9997/v3/config/global/get" | ConvertFrom-Json
    Write-Host "✅ Configuration loaded successfully"
    Write-Host "WebRTC Address: $($config.webrtcAddress)"
    Write-Host "WebRTC Allow Origin: $($config.webrtcAllowOrigin)"
    Write-Host "Authentication Method: $($config.authMethod)"
} catch {
    Write-Host "❌ Config check failed: $($_.Exception.Message)"
}

# Check path status
Write-Host "`nChecking path status..."
try {
    $paths = Invoke-WebRequest -Uri "http://localhost:9997/v3/paths/list" | ConvertFrom-Json
    Write-Host "✅ Paths list successful"
    if ($paths.items) {
        foreach ($path in $paths.items) {
            Write-Host "Path: $($path.name), State: $($path.state), Ready: $($path.ready), Source: $($path.source), SourceReady: $($path.sourceReady)"
        }
    } else {
        Write-Host "No paths configured or found"
    }
} catch {
    Write-Host "❌ Paths list failed: $($_.Exception.Message)"
}