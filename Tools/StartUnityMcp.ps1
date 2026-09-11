$ErrorActionPreference = 'Stop'
$aquaRoot = Split-Path -Parent $PSScriptRoot
$aquaLogDir = Join-Path $aquaRoot 'Logs'
New-Item -ItemType Directory -Path $aquaLogDir -Force | Out-Null
$aquaUvx = Join-Path $env:USERPROFILE '.local/bin/uvx.exe'
if (-not (Test-Path -LiteralPath $aquaUvx)) { throw 'uvx is required. Install uv before running this helper.' }
$aquaListener = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
if (-not $aquaListener) {
    Start-Process -FilePath $aquaUvx -ArgumentList '--from mcpforunityserver==10.0.0 mcp-for-unity --transport http --http-url http://127.0.0.1:8080' -WindowStyle Hidden -RedirectStandardOutput (Join-Path $aquaLogDir 'unity-mcp-server.log') -RedirectStandardError (Join-Path $aquaLogDir 'unity-mcp-server-errors.log')
}
Write-Output 'Unity MCP endpoint: http://127.0.0.1:8080/mcp. In Unity choose Aqua Path > Connect Unity MCP.'
