param([string]$ApiBaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
$health = Invoke-RestMethod "$ApiBaseUrl/health/ready"
if ($health.status -ne 'ready') { throw 'Database not ready.' }
$body = @{ exhibitionIds = @('ocean', 'earth') } | ConvertTo-Json
$saved = Invoke-RestMethod "$ApiBaseUrl/api/itineraries" -Method Post -ContentType 'application/json' -Body $body
$headers = @{ Authorization = "Bearer $($saved.accessToken)" }
$restored = Invoke-RestMethod "$ApiBaseUrl/api/itineraries/$($saved.id)" -Headers $headers
if (($restored.exhibitionIds -join ',') -ne 'ocean,earth') { throw 'Itinerary round trip failed.' }
Write-Output 'PASS: SQL Server itinerary round trip preserved exhibition order.'
# This creates one test itinerary; it expires after 30 days. No token is printed.
