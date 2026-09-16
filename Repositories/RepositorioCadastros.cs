using Dapper;
using JcaInventario.Helpers;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Models;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Repositories;

public partial class RepositorioCadastros : IRepositorioCadastros
{
    private static string ObterTabela(TipoCadastro tipo)
    {
        return tipo switch
        {
            TipoCadastro.Setor => "setores",
            TipoCadastro.TipoEquipamento => "tipos_equipamento",
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };
    }

    public Task<IEnumerable<dynamic>> ListarAsync(SqlConnection conexao, string cadastro)
    {
        var consulta = cadastro switch
        {
            "setores" => "SELECT * FROM setores ORDER BY Nome",
            "tipos" => "SELECT * FROM tipos_equipamento ORDER BY Nome",
            "funcionarios" => "SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId ORDER BY f.Nome",
            "usuarios" => "SELECT Id,Nome,Email,Administrador,Ativo FROM usuarios ORDER BY Nome",
            _ => throw new KeyNotFoundException()
        };
        return conexao.QueryAsync(consulta);
    }

    public Task<int?> ObterCadastroParaAtualizacaoAsync(SqlConnection conexao, TipoCadastro tipo, int id, SqlTransaction transacao)
        => conexao.QuerySingleOrDefaultAsync<int?>($"SELECT Id FROM {ObterTabela(tipo)} WITH (UPDLOCK, HOLDLOCK) WHERE Id=@id", new { id }, transacao);

    public Task<int> ContarVinculosAtivosAsync(SqlConnection conexao, TipoCadastro tipo, int id, SqlTransaction transacao)
    {
        var consulta = tipo == TipoCadastro.Setor
            ? "SELECT COUNT(*) FROM funcionarios WHERE SetorId=@id AND Ativo=1"
            : "SELECT COUNT(*) FROM equipamentos WHERE TipoId=@id AND Status NOT IN ('Inativo','Baixado')";
        return conexao.ExecuteScalarAsync<int>(consulta, new { id }, transacao);
    }

    public Task<int> AtualizarCadastroAsync(SqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, SqlTransaction transacao)
        => conexao.ExecuteAsync($"UPDATE {ObterTabela(tipo)} SET Nome=@Nome,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", cadastro, transacao);

    public Task<int> InserirCadastroAsync(SqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, SqlTransaction transacao)
        => conexao.ExecuteScalarAsync<int>($"INSERT INTO {ObterTabela(tipo)} (Nome,Ativo,Observacoes) VALUES (@Nome,@Ativo,@Observacoes); SELECT CAST(SCOPE_IDENTITY() AS int)", cadastro, transacao);
}
