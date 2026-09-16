using Dapper;
using JcaInventario.Helpers;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Repositories;

public partial class RepositorioCadastros : IRepositorioCadastros
{
    private static string ObterTabela(TipoCadastro tipo)
    {
        return tipo switch
        {
            TipoCadastro.Funcao => "funcoes",
            TipoCadastro.Setor => "setores",
            TipoCadastro.TipoEquipamento => "tipos_equipamento",
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };
    }

    public Task<IEnumerable<dynamic>> ListarAsync(MySqlConnection conexao, string cadastro)
    {
        var consulta = cadastro switch
        {
            "funcoes" => "SELECT * FROM funcoes ORDER BY Nome",
            "setores" => "SELECT * FROM setores ORDER BY Nome",
            "tipos" => "SELECT * FROM tipos_equipamento ORDER BY Nome",
            "funcionarios" => "SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId ORDER BY f.Nome",
            "usuarios" => "SELECT Id,Nome,Email,Administrador,Ativo FROM usuarios ORDER BY Nome",
            _ => throw new KeyNotFoundException()
        };
        return conexao.QueryAsync(consulta);
    }

    public Task<int?> ObterCadastroParaAtualizacaoAsync(MySqlConnection conexao, TipoCadastro tipo, int id, MySqlTransaction transacao)
        => conexao.QuerySingleOrDefaultAsync<int?>($"SELECT Id FROM {ObterTabela(tipo)} WHERE Id=@id FOR UPDATE", new { id }, transacao);

    public Task<int> ContarVinculosAtivosAsync(MySqlConnection conexao, TipoCadastro tipo, int id, MySqlTransaction transacao)
    {
        var consulta = tipo == TipoCadastro.Funcao
            ? "SELECT COUNT(*) FROM funcionarios WHERE FuncaoId=@id AND Ativo=1"
            : tipo == TipoCadastro.Setor
            ? "SELECT COUNT(*) FROM funcionarios WHERE SetorId=@id AND Ativo=1"
            : "SELECT COUNT(*) FROM equipamentos WHERE TipoId=@id AND Status NOT IN ('Inativo','Baixado')";
        return conexao.ExecuteScalarAsync<int>(consulta, new { id }, transacao);
    }

    public Task<int> AtualizarCadastroAsync(MySqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, MySqlTransaction transacao)
        => conexao.ExecuteAsync($"UPDATE {ObterTabela(tipo)} SET Nome=@Nome,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id;" + (tipo == TipoCadastro.Funcao ? " UPDATE funcionarios SET Cargo=@Nome WHERE FuncaoId=@Id AND (Cargo IS NULL OR Cargo<>@Nome);" : ""), cadastro, transacao);

    public Task<int> InserirCadastroAsync(MySqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, MySqlTransaction transacao)
        => conexao.ExecuteScalarAsync<int>($"INSERT INTO {ObterTabela(tipo)} (Nome,Ativo,Observacoes) VALUES (@Nome,@Ativo,@Observacoes); SELECT LAST_INSERT_ID()", cadastro, transacao);
}
