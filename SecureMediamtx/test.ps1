# Test different WHEP URL patterns
$urls = @(
    "http://localhost:8889/whep",              # Standard WHEP endpoint
    "http://localhost:8889/cam1",              # Direct path
    "http://localhost:8889/cam1/",             # With trailing slash
    "http://localhost:8889/cam1/whep",         # Path with /whep
    "http://localhost:9997/v3/webrtc/receivers/cam1/whep"  # API-based WHEP
)

Write-Host "Testing WHEP URLs:`n"

foreach ($url in $urls) {
    try {
        $result = Invoke-WebRequest -Uri $url -Method POST -Body "test" -ContentType "application/sdp" -ErrorAction SilentlyContinue
        Write-Host "✅ $url - Status: $($result.StatusCode)"
        if ($result.StatusCode -eq 200) {
            Write-Host "   Response: $($result.Content.Substring(0, [Math]::Min(100, $result.Content.Length)))..."
        }
    } catch {
        Write-Host "❌ $url - Error: $($_.Exception.Message)"
    }
}