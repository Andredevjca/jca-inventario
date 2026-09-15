using Dapper;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Models;
using MySqlConnector;

namespace JcaInventario.Repositories;

public class RepositorioUsuarios : IRepositorioUsuarios
{
    public Task<CredenciaisUsuario?> ObterCredenciaisAsync(MySqlConnection conexao, string email)
    {
        return conexao.QuerySingleOrDefaultAsync<CredenciaisUsuario>(
            "SELECT Id,Nome,SenhaHash,Administrador FROM usuarios WHERE Email=@email AND Ativo=1",
            new { email });
    }

    public Task<CredenciaisUsuario?> ObterAtivoAsync(MySqlConnection conexao, int id)
    {
        return conexao.QuerySingleOrDefaultAsync<CredenciaisUsuario>(
            "SELECT Id,Nome,Administrador FROM usuarios WHERE Id=@id AND Ativo=1",
            new { id });
    }
}
