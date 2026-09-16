using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioInicializacao
{
    Task CriarBancoAsync();
    Task<int> ObterTravaAsync(SqlConnection conexao);
    Task LiberarTravaAsync(SqlConnection conexao);
    Task CriarEstruturaAsync(SqlConnection conexao, string raiz);
    Task InserirSetorAusenteAsync(SqlConnection conexao, SqlTransaction transacao, string nome);
    Task InserirTipoAusenteAsync(SqlConnection conexao, SqlTransaction transacao, string nome);
    Task<bool> AdministradorExisteAsync(SqlConnection conexao, SqlTransaction transacao);
    Task InserirAdministradorAsync(SqlConnection conexao, SqlTransaction transacao, string senhaHash);
}
