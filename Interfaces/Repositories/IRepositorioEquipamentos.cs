using JcaInventario.Models;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioEquipamentos
{
    Task<IEnumerable<dynamic>> ListarAsync(SqlConnection conexao);
    Task<dynamic?> ObterDetalhesAsync(SqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(SqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarManutencoesAsync(SqlConnection conexao, int id);
    Task<IEnumerable<dynamic>> ListarHistoricoAsync(SqlConnection conexao, int id);
    Task<int> AtualizarFotoAsync(SqlConnection conexao, int id, string? nome, SqlTransaction transacao);
    Task<string?> ObterFotoAsync(SqlConnection conexao, int id);
    Task<int> InserirHistoricoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<bool> ResponsavelAtivoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<bool> TipoAtivoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int?> ObterSetorResponsavelAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirMovimentacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<Equipamento> ObterParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<Equipamento> ObterParaMovimentacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarSituacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<bool> PossuiManutencaoAbertaAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> InserirManutencaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<Manutencao> ObterManutencaoParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
    Task<int> AtualizarManutencaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null);
}
