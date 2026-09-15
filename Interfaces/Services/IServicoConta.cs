using JcaInventario.Models;
using JcaInventario.ViewModels;

namespace JcaInventario.Interfaces.Services;

public interface IServicoConta
{
    Task<UsuarioAutenticado?> AutenticarAsync(LoginViewModel entrada);
    Task<UsuarioAutenticado?> ObterAtivoAsync(int id);
}
