using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioInventario
{
    Task<IEnumerable<JcaInventario.Models.ItemRelatorioInventario>> ListarRelatorioAsync(SqlConnection conexao, int id, SqlTransaction transacao);
    Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarManutencoesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarInventariosAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirConferenciasAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarConferenciasAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterInventarioParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterConferenciaParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarConferenciaAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> ContarPendentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> EncerrarInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<dynamic> ObterTotaisAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorLocalizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> AgruparPorStatusAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> ContarEquipamentosPendentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<IEnumerable<dynamic>> ListarMovimentacoesRecentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
}
