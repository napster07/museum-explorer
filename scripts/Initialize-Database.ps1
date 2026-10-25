$ErrorActionPreference = 'Stop'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location (Join-Path $PSScriptRoot '..')
try { dotnet run --project backend/MuseumExplorer.Api -- --migrate; if ($LASTEXITCODE -ne 0) { throw 'Migration failed. Verify your SQL Server connection.' } }
finally { Pop-Location }
