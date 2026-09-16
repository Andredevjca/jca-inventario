$ErrorActionPreference = 'Stop'
$config = Get-Content (Join-Path $PSScriptRoot '../appsettings.Local.json') -Raw | ConvertFrom-Json
$conexao = New-Object System.Data.SqlClient.SqlConnection($config.ConnectionStrings.Banco)
try {
    $conexao.Open()
    $cmd = $conexao.CreateCommand()
    $cmd.CommandTimeout = 120
    $cmd.CommandText = 'BEGIN TRANSACTION'
    [void]$cmd.ExecuteNonQuery()
    $migracao = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../Database/funcionarios-auditoria.sql'))
    foreach ($execucao in 1..2) {
        $cmd.CommandText = $migracao
        [void]$cmd.ExecuteNonQuery()
    }
    $cmd.CommandText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'auditoria.sql'))
    $total = $cmd.ExecuteScalar()
    Write-Output "OK: migração executada duas vezes; inclusão, alteração e autoria verificadas em $total tabelas."
} finally {
    if ($conexao.State -eq 'Open') {
        $rollback = $conexao.CreateCommand()
        $rollback.CommandText = 'IF @@TRANCOUNT > 0 ROLLBACK'
        [void]$rollback.ExecuteNonQuery()
    }
    $conexao.Dispose()
}
