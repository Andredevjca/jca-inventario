using Dapper;
using MySqlConnector;
using JcaInventario.Servicos;
using System.Text.RegularExpressions;
namespace JcaInventario.Infraestrutura;

public static class InicializadorBanco
{
    public static async Task InicializarAsync(Banco banco, string raiz)
    {
        var configuracao = new MySqlConnectionStringBuilder(banco.Conexao);
        var nome = configuracao.Database;
        if (!Regex.IsMatch(nome, "^[a-zA-Z0-9_]{1,64}$")) throw new InvalidOperationException("Nome do banco inválido.");
        configuracao.Database = "";
        await using var servidor = new MySqlConnection(configuracao.ConnectionString);
        await servidor.OpenAsync();
        await servidor.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{nome}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
        await using var conexao = await banco.AbrirAsync();
        var trava = await conexao.ExecuteScalarAsync<int>("SELECT GET_LOCK(@Nome, 60)", new { Nome = nome + "_inicializacao" });
        if (trava != 1) throw new InvalidOperationException("Não foi possível obter a trava de inicialização.");
        try {
            var estrutura = await File.ReadAllTextAsync(Path.Combine(raiz, "Infraestrutura", "estrutura.sql"));
            await conexao.ExecuteAsync(estrutura);
            await using var transacao = await conexao.BeginTransactionAsync();
            foreach (var setor in new[] { "Desenvolvimento", "Suporte", "Financeiro", "Comercial", "Administrativo", "Recursos Humanos" })
                await conexao.ExecuteAsync("INSERT INTO setores (Nome) SELECT @Nome WHERE NOT EXISTS (SELECT 1 FROM setores WHERE Nome=@Nome)", new { Nome = setor }, transacao);
            foreach (var tipo in new[] { "Notebook", "Desktop", "Monitor", "Teclado", "Mouse", "Headset", "Impressora", "Celular", "Tablet", "Nobreak", "Outros" })
                await conexao.ExecuteAsync("INSERT INTO tipos_equipamento (Nome) SELECT @Nome WHERE NOT EXISTS (SELECT 1 FROM tipos_equipamento WHERE Nome=@Nome)", new { Nome = tipo }, transacao);
            if (!await conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM usuarios WHERE Email='admin@admin.com')", transaction: transacao))
                await conexao.ExecuteAsync("INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador) VALUES ('Administrador','admin@admin.com',@SenhaHash,1)", new { SenhaHash = Senhas.Gerar("admin") }, transacao);
            await transacao.CommitAsync();
        } finally { await conexao.ExecuteAsync("SELECT RELEASE_LOCK(@Nome)", new { Nome = nome + "_inicializacao" }); }
    }
}
