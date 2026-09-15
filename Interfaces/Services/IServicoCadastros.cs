using JcaInventario.Helpers;
using JcaInventario.Models;

namespace JcaInventario.Interfaces.Services;

public interface IServicoCadastros
{
    object Opcoes();
    Task<object> ListarAsync(string cadastro);
    Task<object> SalvarCadastroAsync(TipoCadastro tipo, Cadastro cadastro);
    Task<object> SalvarFuncionarioAsync(Funcionario funcionario, int usuario);
    Task<object> ObterFuncionarioAsync(int id);
    Task<object> ObterSetorAsync(int id);
    Task<object> SalvarUsuarioAsync(Usuario usuario, int usuarioAtual);
}
