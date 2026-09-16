using JcaInventario.Models;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Repositories;

public interface IRepositorioUsuarios
{
    Task<CredenciaisUsuario?> ObterCredenciaisAsync(SqlConnection conexao, string email);
    Task<CredenciaisUsuario?> ObterAtivoAsync(SqlConnection conexao, int id);
}
