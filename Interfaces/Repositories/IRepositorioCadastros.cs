using JcaInventario.Helpers;
using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioCadastros
{
    Task<IEnumerable<dynamic>> ListarAsync(MySqlConnection conexao, string cadastro);
    Task<int?> ObterCadastroParaAtualizacaoAsync(MySqlConnection conexao, TipoCadastro tipo, int id, MySqlTransaction transacao);
    Task<int> ContarVinculosAtivosAsync(MySqlConnection conexao, TipoCadastro tipo, int id, MySqlTransaction transacao);
    Task<int> AtualizarCadastroAsync(MySqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, MySqlTransaction transacao);
    Task<int> InserirCadastroAsync(MySqlConnection conexao, TipoCadastro tipo, Cadastro cadastro, MySqlTransaction transacao);
    Task<Cadastro?> ObterFuncaoAsync(MySqlConnection conexao, int id, MySqlTransaction transacao);
    Task<bool> SetorAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<Funcionario> ObterFuncionarioParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> ContarEquipamentosResponsavelAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<Equipamento>> ListarEquipamentosResponsavelParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirTransferenciaSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarEquipamentosFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarHistoricoFuncionarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarFuncionariosSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarEquipamentosSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirUsuarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarUsuarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
}
