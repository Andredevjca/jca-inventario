using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioUsuarios
{
    Task<CredenciaisUsuario?> ObterCredenciaisAsync(MySqlConnection conexao, string email);
    Task<CredenciaisUsuario?> ObterAtivoAsync(MySqlConnection conexao, int id);
}
