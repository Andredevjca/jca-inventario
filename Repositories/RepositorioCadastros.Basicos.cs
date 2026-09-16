using Dapper;
using JcaInventario.Models;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Repositories;

public partial class RepositorioCadastros
{
    public Task<bool> SetorAtivoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM setores WHERE Id=@SetorId AND Ativo=1) THEN 1 ELSE 0 END", parametros, transacao);

    public Task<Funcionario> ObterFuncionarioParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync<Funcionario>("SELECT * FROM funcionarios WITH (UPDLOCK, HOLDLOCK) WHERE Id=@Id", parametros, transacao);

    public Task<int> ContarEquipamentosResponsavelAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM equipamentos WHERE ResponsavelId=@Id", parametros, transacao);

    public Task<int> InserirFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO funcionarios (Nome,Email,Telefone,Matricula,Cargo,SetorId,TipoTrabalho,Ativo,Observacoes) VALUES (@Nome,@Email,@Telefone,@Matricula,@Cargo,@SetorId,@TipoTrabalho,@Ativo,@Observacoes); SELECT CAST(SCOPE_IDENTITY() AS int)", parametros, transacao);

    public Task<IEnumerable<Equipamento>> ListarEquipamentosResponsavelParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync<Equipamento>("SELECT * FROM equipamentos WITH (UPDLOCK, HOLDLOCK) WHERE ResponsavelId=@Id", parametros, transacao);

    public Task<int> InserirTransferenciaSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("""
                        INSERT INTO movimentacoes (EquipamentoId,ResponsavelAnteriorId,NovoResponsavelId,SetorAnteriorId,NovoSetorId,LocalizacaoAnterior,NovaLocalizacao,StatusAnterior,NovoStatus,Tipo,UsuarioId,Observacao)
                        VALUES (@equipamentoId,@funcionarioId,@funcionarioId,@setorAnterior,@setorNovo,@local,@local,@status,@status,'Transferência de setor',@usuario,'Alteração do setor do funcionário')
                        """, parametros, transacao);

    public Task<int> AtualizarFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE funcionarios SET Nome=@Nome,Email=@Email,Telefone=@Telefone,Matricula=@Matricula,Cargo=@Cargo,SetorId=@SetorId,TipoTrabalho=@TipoTrabalho,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", parametros, transacao);

    public Task<dynamic> ObterFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId WHERE f.Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarEquipamentosFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE e.ResponsavelId=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarHistoricoFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " WHERE m.ResponsavelAnteriorId=@id OR m.NovoResponsavelId=@id ORDER BY m.Id DESC", parametros, transacao);

    public Task<dynamic> ObterSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM setores WHERE Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarFuncionariosSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT * FROM funcionarios WHERE SetorId=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarEquipamentosSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE f.SetorId=@id", parametros, transacao);

    public Task<int> InserirUsuarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador,Ativo) VALUES (@Nome,@Email,@resumo,@Administrador,@Ativo); SELECT CAST(SCOPE_IDENTITY() AS int)", parametros, transacao);

    public Task<int> AtualizarUsuarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE usuarios SET Nome=@Nome,Email=@Email,SenhaHash=COALESCE(@resumo,SenhaHash),Administrador=@Administrador,Ativo=@Ativo WHERE Id=@Id", parametros, transacao);
}
