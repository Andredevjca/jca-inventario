param()
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet publish .\JcaInventario.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o .\publicacao\win-x64
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar a aplicacao.' }
Write-Host 'Publicacao pronta em publicacao\win-x64. Copie todo o conteudo para a pasta do site no IIS.'
