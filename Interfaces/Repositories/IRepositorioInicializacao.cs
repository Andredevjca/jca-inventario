using MySqlConnector;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioInicializacao
{
    Task CriarBancoAsync();
    Task<int> ObterTravaAsync(MySqlConnection conexao);
    Task LiberarTravaAsync(MySqlConnection conexao);
    Task CriarEstruturaAsync(MySqlConnection conexao, string raiz);
    Task InserirSetorAusenteAsync(MySqlConnection conexao, MySqlTransaction transacao, string nome);
    Task InserirTipoAusenteAsync(MySqlConnection conexao, MySqlTransaction transacao, string nome);
    Task<bool> AdministradorExisteAsync(MySqlConnection conexao, MySqlTransaction transacao);
    Task InserirAdministradorAsync(MySqlConnection conexao, MySqlTransaction transacao, string senhaHash);
}
