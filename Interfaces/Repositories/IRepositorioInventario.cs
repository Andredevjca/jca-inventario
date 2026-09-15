using MySqlConnector;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioInventario
{
    Task<IEnumerable<JcaInventario.Models.ItemRelatorioInventario>> ListarRelatorioAsync(MySqlConnection conexao, int id, MySqlTransaction transacao);
    Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarManutencoesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarInventariosAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> InserirConferenciasAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarConferenciasAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterInventarioParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterConferenciaParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> AtualizarConferenciaAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> ContarPendentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> EncerrarInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<dynamic> ObterTotaisAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorLocalizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorStatusAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<int> ContarEquipamentosPendentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarMovimentacoesRecentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null);
}
