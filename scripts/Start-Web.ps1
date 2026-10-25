$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../frontend')
try { npm.cmd run dev; if ($LASTEXITCODE -ne 0) { throw 'Frontend failed.' } }
finally { Pop-Location }
