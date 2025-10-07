# Get token
$tokenResponse = Invoke-WebRequest -Uri "http://localhost:5046/api/stream/token" -Method POST -Body '{"cameraId": "cam1"}' -ContentType "application/json"
$tokenData = $tokenResponse.Content | ConvertFrom-Json
$accessToken = $tokenData.accessToken

Write-Host "Token: $accessToken"

# Comprehensive SDP offer for MediaMTX WebRTC
$sdpOffer = @"
v=0
o=- 0 0 IN IP4 0.0.0.0
s=-
t=0 0
a=group:BUNDLE 0
a=extmap-allow-mixed
a=msid-semantic: WMS
m=video 9 UDP/TLS/RTP/SAVPF 96
c=IN IP4 0.0.0.0
a=rtcp:9 IN IP4 0.0.0.0
a=ice-ufrag:$(Get-Random)
a=ice-pwd:$(Get-Random -Minimum 1000000000 -Maximum 9999999999)
a=ice-options:trickle
a=fingerprint:sha-256 00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00:00
a=setup:actpass
a=mid:0
a=recvonly
a=rtcp-mux
a=rtcp-rsize
a=rtpmap:96 H264/90000
a=fmtp:96 packetization-mode=1;profile-level-id=640028
a=ssrc:1234 cname:stream
"@

Write-Host "Testing WebRTC with comprehensive SDP..."

try {
    $response = Invoke-WebRequest -Uri "http://localhost:8889/cam1/whep?token=$accessToken" -Method POST -Body $sdpOffer -ContentType "application/sdp" -TimeoutSec 30
    Write-Host "✅✅✅ SUCCESS! WebRTC connection established!"
    Write-Host "SDP Answer:"
    Write-Host $response.Content
} catch {
    Write-Host "❌ WebRTC failed: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)"
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody"
    }
}