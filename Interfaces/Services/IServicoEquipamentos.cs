using JcaInventario.Helpers;
using JcaInventario.Models;
using JcaInventario.ViewModels;
using MySqlConnector;

namespace JcaInventario.Interfaces.Services;

public interface IServicoEquipamentos
{
    Task<int> SalvarAsync(Equipamento equipamento, int usuario);
    Task MovimentarAsync(int id, MovimentacaoViewModel movimento, int usuario);
    Task<object> ListarAsync();
    Task<object> ObterDetalhesAsync(int id);
    Task RegistrarHistoricoAsync(MySqlConnection conexao, MySqlTransaction transacao, int id, int usuario, string descricao, object? anterior, object? novo);
    Task RegistrarMovimentacaoAsync(MySqlConnection conexao, MySqlTransaction transacao, Equipamento? anterior, Equipamento novo, int usuario, string tipo, string? observacao);
}
