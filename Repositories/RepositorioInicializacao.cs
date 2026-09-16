using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace JcaInventario.Repositories;

public class RepositorioInicializacao : IRepositorioInicializacao
{
    private readonly Banco _conexao;

    public RepositorioInicializacao(Banco conexao)
    {
        _conexao = conexao;
    }

    public async Task CriarBancoAsync()
    {
        var configuracao = new SqlConnectionStringBuilder(_conexao.Conexao);
        var nome = configuracao.InitialCatalog;
        if (!Regex.IsMatch(nome, "^[a-zA-Z0-9_]{1,64}$"))
        {
            throw new InvalidOperationException("Nome do banco inválido.");
        }

        configuracao.InitialCatalog = "master";
        await using var servidor = new SqlConnection(configuracao.ConnectionString);
        await servidor.OpenAsync();
        await servidor.ExecuteAsync($"IF DB_ID(@nome) IS NULL EXEC(N'CREATE DATABASE [{nome}]')", new { nome });
    }

    public Task<int> ObterTravaAsync(SqlConnection conexao)
    {
        return conexao.ExecuteScalarAsync<int>("""
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock @Resource=@Nome, @LockMode='Exclusive',
                @LockOwner='Session', @LockTimeout=60000;
            SELECT CASE WHEN @resultado >= 0 THEN 1 ELSE 0 END;
            """, new { Nome = conexao.Database + "_inicializacao" }, commandTimeout: 70);
    }

    public Task LiberarTravaAsync(SqlConnection conexao)
    {
        return conexao.ExecuteAsync("EXEC sys.sp_releaseapplock @Resource=@Nome, @LockOwner='Session'", new { Nome = conexao.Database + "_inicializacao" });
    }

    public async Task CriarEstruturaAsync(SqlConnection conexao, string raiz)
    {
        var estrutura = await File.ReadAllTextAsync(Path.Combine(raiz, "Database", "estrutura.sql"));
        await conexao.ExecuteAsync(estrutura);
        var ajustes = await File.ReadAllTextAsync(Path.Combine(raiz, "Database", "funcionarios-auditoria.sql"));
        await conexao.ExecuteAsync(ajustes, commandTimeout: 120);
    }

    public Task InserirSetorAusenteAsync(SqlConnection conexao, SqlTransaction transacao, string nome)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO setores (Nome) SELECT @nome WHERE NOT EXISTS (SELECT 1 FROM setores WHERE Nome=@nome)",
            new { nome },
            transacao);
    }

    public Task InserirTipoAusenteAsync(SqlConnection conexao, SqlTransaction transacao, string nome)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO tipos_equipamento (Nome) SELECT @nome WHERE NOT EXISTS (SELECT 1 FROM tipos_equipamento WHERE Nome=@nome)",
            new { nome },
            transacao);
    }

    public Task<bool> AdministradorExisteAsync(SqlConnection conexao, SqlTransaction transacao)
    {
        return conexao.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM usuarios WHERE Email='admin@admin.com') THEN 1 ELSE 0 END", transaction: transacao);
    }

    public Task InserirAdministradorAsync(SqlConnection conexao, SqlTransaction transacao, string senhaHash)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador) VALUES ('Administrador','admin@admin.com',@senhaHash,1)",
            new { senhaHash },
            transacao);
    }
}
