$ErrorActionPreference = 'Stop'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location (Join-Path $PSScriptRoot '..')
try { dotnet run --project backend/MuseumExplorer.Api -- --urls http://127.0.0.1:5080; if ($LASTEXITCODE -ne 0) { throw 'API failed.' } }
finally { Pop-Location }
