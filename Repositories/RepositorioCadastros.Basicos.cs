using Dapper;
using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Repositories;

public partial class RepositorioCadastros
{
    public Task<bool> SetorAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM setores WHERE Id=@SetorId AND Ativo=1)", parametros, transacao);

    public Task<Funcionario> ObterFuncionarioParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync<Funcionario>("SELECT * FROM funcionarios WHERE Id=@Id FOR UPDATE", parametros, transacao);

    public Task<int> ContarEquipamentosResponsavelAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM equipamentos WHERE ResponsavelId=@Id", parametros, transacao);

    public Task<int> InserirFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO funcionarios (Nome,Email,Telefone,Matricula,Cargo,SetorId,TipoTrabalho,Ativo,Observacoes) VALUES (@Nome,@Email,@Telefone,@Matricula,@Cargo,@SetorId,@TipoTrabalho,@Ativo,@Observacoes); SELECT LAST_INSERT_ID()", parametros, transacao);

    public Task<IEnumerable<Equipamento>> ListarEquipamentosResponsavelParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync<Equipamento>("SELECT * FROM equipamentos WHERE ResponsavelId=@Id FOR UPDATE", parametros, transacao);

    public Task<int> InserirTransferenciaSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("""
                        INSERT INTO movimentacoes (EquipamentoId,ResponsavelAnteriorId,NovoResponsavelId,SetorAnteriorId,NovoSetorId,LocalizacaoAnterior,NovaLocalizacao,StatusAnterior,NovoStatus,Tipo,UsuarioId,Observacao)
                        VALUES (@equipamentoId,@funcionarioId,@funcionarioId,@setorAnterior,@setorNovo,@local,@local,@status,@status,'Transferência de setor',@usuario,'Alteração do setor do funcionário')
                        """, parametros, transacao);

    public Task<int> AtualizarFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE funcionarios SET Nome=@Nome,Email=@Email,Telefone=@Telefone,Matricula=@Matricula,Cargo=@Cargo,SetorId=@SetorId,TipoTrabalho=@TipoTrabalho,Ativo=@Ativo,Observacoes=@Observacoes WHERE Id=@Id", parametros, transacao);

    public Task<dynamic> ObterFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT f.*,s.Nome Setor FROM funcionarios f JOIN setores s ON s.Id=f.SetorId WHERE f.Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarEquipamentosFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE e.ResponsavelId=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarHistoricoFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " WHERE m.ResponsavelAnteriorId=@id OR m.NovoResponsavelId=@id ORDER BY m.Id DESC", parametros, transacao);

    public Task<dynamic> ObterSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM setores WHERE Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarFuncionariosSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT * FROM funcionarios WHERE SetorId=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarEquipamentosSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync(RepositorioInventario.ConsultaEquipamentos + " WHERE f.SetorId=@id", parametros, transacao);

    public Task<int> InserirUsuarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO usuarios (Nome,Email,SenhaHash,Administrador,Ativo) VALUES (@Nome,@Email,@resumo,@Administrador,@Ativo); SELECT LAST_INSERT_ID()", parametros, transacao);

    public Task<int> AtualizarUsuarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE usuarios SET Nome=@Nome,Email=@Email,SenhaHash=COALESCE(@resumo,SenhaHash),Administrador=@Administrador,Ativo=@Ativo WHERE Id=@Id", parametros, transacao);
}
