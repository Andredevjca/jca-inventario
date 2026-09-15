using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioEquipamentos
{
    Task<IEnumerable<dynamic>> ListarAsync(MySqlConnection conexao);
    Task<dynamic?> ObterDetalhesAsync(MySqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(MySqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarManutencoesAsync(MySqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarHistoricoAsync(MySqlConnection conexao, int id);
    Task<int> AtualizarFotoAsync(MySqlConnection conexao, int id, string? nome, MySqlTransaction transacao);
    Task<string?> ObterFotoAsync(MySqlConnection conexao, int id);
    Task<int> InserirHistoricoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<bool> ResponsavelAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<bool> TipoAtivoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int?> ObterSetorResponsavelAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirMovimentacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<Equipamento> ObterParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<Equipamento> ObterParaMovimentacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarSituacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<bool> PossuiManutencaoAbertaAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirManutencaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<Manutencao> ObterManutencaoParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarManutencaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
}
