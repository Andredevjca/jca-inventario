param([int]$PortaAplicacao = 5085)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
Write-Host 'Iniciando com o SQL Server configurado em ConnectionStrings:Banco...'
dotnet run --no-launch-profile --urls "http://localhost:$PortaAplicacao"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao iniciar a aplicacao.' }

