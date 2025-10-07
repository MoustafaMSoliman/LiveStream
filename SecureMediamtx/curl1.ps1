Invoke-WebRequest `
  -Uri "http://localhost:5046/api/stream/validate-token" `
  -Method POST `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body '{
    "path": "cam1",
    "ip": "172.18.0.1",
    "action": "read",
    "protocol": "webrtc",
    "query": "token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJjYW1lcmFJZCI6ImNhbTEiLCJ1c2VySWQiOiJhbm9ueW1vdXMiLCJjbGllbnRJcCI6IjE3Mi4xOC4wLjEiLCJ0b2tlblR5cGUiOiJhY2Nlc3MiLCJleHAiOjE3NTk4NDI0MzcsIm5iZiI6MTc1OTg0MjEzNywiaWF0IjoxNzU5ODQyMTM3fQ.7tPNXnjpVZ6G0zOufpbthloDU7hE0ONXgL0KE4xf7wg"
  }'
