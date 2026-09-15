using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.Repositories;
using JcaInventario.ViewModels;

namespace JcaInventario.Servicos;

public class ServicoConta(Banco banco, IRepositorioUsuarios repositorio) : IServicoConta
{
    public async Task<UsuarioAutenticado?> AutenticarAsync(LoginViewModel entrada)
    {
        await using var conexao = await banco.AbrirAsync();
        var usuario = await repositorio.ObterCredenciaisAsync(conexao, entrada.Email);
        if (usuario == null || !Senhas.Verificar(entrada.Senha, usuario.SenhaHash)) return null;
        return new UsuarioAutenticado(usuario.Id, usuario.Nome, usuario.Administrador);
    }

    public async Task<UsuarioAutenticado?> ObterAtivoAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var usuario = await repositorio.ObterAtivoAsync(conexao, id);
        return usuario == null ? null : new UsuarioAutenticado(usuario.Id, usuario.Nome, usuario.Administrador);
    }
}
