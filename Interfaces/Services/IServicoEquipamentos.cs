using JcaInventario.Helpers;
using JcaInventario.Models;
using JcaInventario.ViewModels;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Interfaces.Services;

public interface IServicoEquipamentos
{
    Task<int> SalvarAsync(Equipamento equipamento, int usuario);
    Task MovimentarAsync(int id, MovimentacaoViewModel movimento, int usuario);
    Task<object> ListarAsync();
    Task<object> ObterDetalhesAsync(int id);
    Task RegistrarHistoricoAsync(SqlConnection conexao, SqlTransaction transacao, int id, int usuario, string descricao, object? anterior, object? novo);
    Task RegistrarMovimentacaoAsync(SqlConnection conexao, SqlTransaction transacao, Equipamento? anterior, Equipamento novo, int usuario, string tipo, string? observacao);
}
