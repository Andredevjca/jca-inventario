using JcaInventario.ViewModels;

namespace JcaInventario.Interfaces.Services;

public interface IServicoInventario
{
    Task<object> ListarMovimentacoesAsync();
    Task<object> ListarManutencoesAsync();
    Task<object> ListarInventariosAsync();
    Task<object> CriarInventarioAsync(NovoInventarioViewModel inventario, int usuario);
    Task<object> ObterConferenciasAsync(int id);
    Task ConferirAsync(int id, int equipamentoId, ConferenciaViewModel conferencia, int usuario);
    Task EncerrarAsync(int id);
    Task<object> ObterDashboardAsync();
}
