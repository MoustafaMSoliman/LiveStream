# Check the base API endpoint
Write-Host "Testing API base endpoint..."
try {
    $apiBase = Invoke-WebRequest -Uri "http://localhost:9997/" -Method GET
    Write-Host "✅ API base response: $($apiBase.StatusDescription)"
} catch {
    Write-Host "❌ API base failed: $($_.Exception.Message)"
}

# Try different API versions
Write-Host "`nTrying different API endpoints..."
$endpoints = @(
    "/v2/config/global/get",
    "/v3/config/global/get", 
    "/config/global/get",
    "/v2/paths/list",
    "/v3/paths/list",
    "/paths/list"
)

foreach ($endpoint in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9997$endpoint" -Method GET
        Write-Host "✅ $endpoint - Success: $($response.StatusDescription)"
        break
    } catch {
        Write-Host "❌ $endpoint - Failed: $($_.Exception.Response.StatusCode)"
    }
}