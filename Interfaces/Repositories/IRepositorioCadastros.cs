using JcaInventario.Helpers;
using JcaInventario.Models;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioCadastros
{
    Task<IEnumerable<dynamic>> ListarAsync(SqlConnection conexao, string cadastro);
    Task<int?> ObterCadastroParaAtualizacaoAsync(SqlConnection conexao, TipoCadastro tipo, int id, SqlTransaction transacao);
    Task<int> ContarVinculosAtivosAsync(SqlConnection conexao, TipoCadastro tipo, int id, SqlTransaction transacao);
    Task<int> AtualizarCadastroAsync(SqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, SqlTransaction transacao);
    Task<int> InserirCadastroAsync(SqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, SqlTransaction transacao);
    Task<Cadastro?> ObterFuncaoAsync(SqlConnection conexao, int id, SqlTransaction transacao);
    Task<bool> SetorAtivoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<Funcionario> ObterFuncionarioParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> ContarEquipamentosResponsavelAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<Equipamento>> ListarEquipamentosResponsavelParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirTransferenciaSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarEquipamentosFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarHistoricoFuncionarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarFuncionariosSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarEquipamentosSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirUsuarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarUsuarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
}
