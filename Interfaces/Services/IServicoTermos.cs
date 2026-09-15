using JcaInventario.ViewModels;

namespace JcaInventario.Interfaces.Services;

public interface IServicoTermos
{
    Task<ArquivoGerado> GerarAsync(int id);
}
