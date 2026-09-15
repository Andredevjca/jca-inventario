using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using MySqlConnector;
using System.Text.RegularExpressions;

namespace JcaInventario.Repositories;

public class RepositorioInicializacao : IRepositorioInicializacao
{
    private readonly ConexaoBanco _conexao;

    public RepositorioInicializacao(ConexaoBanco conexao)
    {
        _conexao = conexao;
    }

    public async Task CriarBancoAsync()
    {
        var configuracao = new MySqlConnectionStringBuilder(_conexao.Conexao);
        var nome = configuracao.Database;
        if (!Regex.IsMatch(nome, "^[a-zA-Z0-9_]{1,64}$"))
        {
            throw new InvalidOperationException("Nome do banco inválido.");
        }

        configuracao.Database = "";
        await using var servidor = new MySqlConnection(configuracao.ConnectionString);
        await servidor.OpenAsync();
        await servidor.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{nome}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
    }

    public Task<int> ObterTravaAsync(MySqlConnection conexao)
    {
        return conexao.ExecuteScalarAsync<int>("SELECT GET_LOCK(@Nome, 60)", new { Nome = conexao.Database + "_inicializacao" });
    }

    public Task LiberarTravaAsync(MySqlConnection conexao)
    {
        return conexao.ExecuteAsync("SELECT RELEASE_LOCK(@Nome)", new { Nome = conexao.Database + "_inicializacao" });
    }

    public async Task CriarEstruturaAsync(MySqlConnection conexao, string raiz)
    {
        var estrutura = await File.ReadAllTextAsync(Path.Combine(raiz, "Database", "estrutura.sql"));
        await conexao.ExecuteAsync(estrutura);
    }

    public Task InserirSetorAusenteAsync(MySqlConnection conexao, MySqlTransaction transacao, string nome)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO setores (Nome) SELECT @nome WHERE NOT EXISTS (SELECT 1 FROM setores WHERE Nome=@nome)",
            new { nome },
            transacao);
    }

    public Task InserirTipoAusenteAsync(MySqlConnection conexao, MySqlTransaction transacao, string nome)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO tipos_equipamento (Nome) SELECT @nome WHERE NOT EXISTS (SELECT 1 FROM tipos_equipamento WHERE Nome=@nome)",
            new { nome },
            transacao);
    }

    public Task<bool> AdministradorExisteAsync(MySqlConnection conexao, MySqlTransaction transacao)
    {
        return conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM usuarios WHERE Email='admin@admin.com')", transaction: transacao);
    }

    public Task InserirAdministradorAsync(MySqlConnection conexao, MySqlTransaction transacao, string senhaHash)
    {
        return conexao.ExecuteAsync(
            "INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador) VALUES ('Administrador','admin@admin.com',@senhaHash,1)",
            new { senhaHash },
            transacao);
    }
}
