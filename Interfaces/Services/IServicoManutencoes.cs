using JcaInventario.Models;

namespace JcaInventario.Interfaces.Services;

public interface IServicoManutencoes
{
    Task<int> SalvarAsync(Manutencao manutencao, int usuario);
}
