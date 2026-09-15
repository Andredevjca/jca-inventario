param(
    [string]$ExecutavelMySql = 'C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysqld.exe',
    [int]$PortaBanco = 3309,
    [int]$PortaAplicacao = 5085,
    [switch]$SomenteBanco
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if (!(Test-Path -LiteralPath $ExecutavelMySql)) { throw 'MySQL não encontrado. Informe -ExecutavelMySql com o caminho de mysqld.exe, ou configure um servidor existente conforme o README.' }
$pastaExecutavel = Split-Path -Parent $ExecutavelMySql
$pastaMySql = Split-Path -Parent $pastaExecutavel
$pastaDados = Join-Path $PSScriptRoot 'Dados\mysql-local'
$arquivoCredenciais = Join-Path $PSScriptRoot 'Dados\mysql-local.json'
New-Item -ItemType Directory -Path $pastaDados -Force | Out-Null
if (!(Test-Path -LiteralPath $arquivoCredenciais)) {
    $aleatorio = New-Object byte[] 32
    $gerador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $gerador.GetBytes($aleatorio)
    $gerador.Dispose()
    $senha = ([BitConverter]::ToString($aleatorio)).Replace('-','').ToLowerInvariant()
    @{ senha = $senha; porta = $PortaBanco; configurado = $false } | ConvertTo-Json | Set-Content -LiteralPath $arquivoCredenciais -Encoding UTF8
}
$credenciais = Get-Content -LiteralPath $arquivoCredenciais -Raw | ConvertFrom-Json
if ($credenciais.porta -ne $PortaBanco) { throw "Esta instalação local utiliza a porta $($credenciais.porta). Informe a mesma porta." }
if (!(Test-Path -LiteralPath (Join-Path $pastaDados 'mysql'))) {
    Write-Host 'Preparando o servidor MySQL local...'
    & $ExecutavelMySql --no-defaults --initialize-insecure "--basedir=$pastaMySql" "--datadir=$pastaDados" --console
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao inicializar o servidor MySQL local.' }
}
$cliente = Join-Path $pastaExecutavel 'mysql.exe'
$administrador = Join-Path $pastaExecutavel 'mysqladmin.exe'
$argumentos = @('--no-defaults',"--basedir=`"$pastaMySql`"","--datadir=`"$pastaDados`"","--port=$PortaBanco",'--bind-address=127.0.0.1','--mysqlx=OFF','--console')
$processo = $null
$senhaAnterior = $env:MYSQL_PWD
try {
    $portaOcupada = Get-NetTCPConnection -State Listen -LocalPort $PortaBanco -ErrorAction SilentlyContinue
    if ($portaOcupada) { throw "A porta $PortaBanco já está em uso. Encerre a execução anterior deste script ou utilize um servidor configurado no README." }
    $processo = Start-Process -FilePath $ExecutavelMySql -ArgumentList $argumentos -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $PSScriptRoot 'Dados\mysql-local-saida.log') -RedirectStandardError (Join-Path $PSScriptRoot 'Dados\mysql-local-erros.log')
    $pronto = $false
    for ($tentativa = 0; $tentativa -lt 100; $tentativa++) {
        if ($processo.HasExited) { throw 'MySQL encerrou antes de ficar disponível. Consulte Dados\mysql-local-erros.log.' }
        $conexao = New-Object System.Net.Sockets.TcpClient
        try { $conexao.Connect('127.0.0.1',$PortaBanco); $pronto = $true; break } catch { Start-Sleep -Milliseconds 200 } finally { $conexao.Dispose() }
    }
    if (!$pronto) { throw 'MySQL não ficou disponível a tempo.' }
    if (!$credenciais.configurado) {
        $env:MYSQL_PWD = ''
        "ALTER USER 'root'@'localhost' IDENTIFIED BY '$($credenciais.senha)';" | & $cliente --no-defaults --host=127.0.0.1 "--port=$PortaBanco" --user=root --ssl-mode=DISABLED
        if ($LASTEXITCODE -ne 0) { throw 'Não foi possível definir a senha do MySQL local.' }
        $credenciais.configurado = $true
        $credenciais | ConvertTo-Json | Set-Content -LiteralPath $arquivoCredenciais -Encoding UTF8
    }
    $arquivoConfiguracao = Join-Path $PSScriptRoot 'appsettings.Local.json'
    if (!(Test-Path -LiteralPath $arquivoConfiguracao)) {
        @{ ConnectionStrings = @{ Banco = "Server=127.0.0.1;Port=$PortaBanco;Database=jca_inventario;User ID=root;Password=$($credenciais.senha);SslMode=None;AllowPublicKeyRetrieval=True" } } | ConvertTo-Json | Set-Content -LiteralPath $arquivoConfiguracao -Encoding UTF8
    }
    $env:MYSQL_PWD = $senhaAnterior
    if ($SomenteBanco) {
        Write-Host "MySQL local disponível na porta $PortaBanco. Execute JcaInventario.slnx no Visual Studio."
        Read-Host 'Mantenha este terminal aberto. Pressione Enter para desligar o banco' | Out-Null
    } else {
        Write-Host "Iniciando JCA Inventário em http://localhost:$PortaAplicacao"
        Write-Host 'A aplicação cria o banco, as tabelas e o administrador se ainda não existirem.'
        & dotnet run --project (Join-Path $PSScriptRoot 'JcaInventario.csproj') --launch-profile JcaInventario --urls "http://localhost:$PortaAplicacao"
    }
} finally {
    if ($processo -and !$processo.HasExited) {
        $env:MYSQL_PWD = if ($credenciais.configurado) { $credenciais.senha } else { '' }
        & $administrador --no-defaults --host=127.0.0.1 "--port=$PortaBanco" --user=root --ssl-mode=DISABLED shutdown
    }
    $env:MYSQL_PWD = $senhaAnterior
}

