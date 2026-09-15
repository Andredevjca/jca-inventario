using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : PainelController
{
    private readonly IServicoCadastros _servico;

    public UsuariosController(IServicoCadastros servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Usuários";
        return View(Formatador.Lista(await _servico.ListarAsync("usuarios")));
    }

    public IActionResult Criar()
    {
        ViewData["Title"] = "Novo usuário";
        return View("Formulario", new Usuario());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Usuario modelo)
    {
        return await Salvar(modelo, "Novo usuário");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar usuário";
        var item = Formatador.Lista(await _servico.ListarAsync("usuarios")).FirstOrDefault(usuario => Convert.ToInt32(usuario.Id) == id);
        if (item == null) return NotFound();
        return View("Formulario", new Usuario
        {
            Id = Convert.ToInt32(item.Id),
            Nome = Convert.ToString(item.Nome) ?? "",
            Email = Convert.ToString(item.Email) ?? "",
            Administrador = Convert.ToBoolean(item.Administrador),
            Ativo = Convert.ToBoolean(item.Ativo)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Usuario modelo)
    {
        modelo.Id = id;
        return await Salvar(modelo, "Editar usuário");
    }

    private async Task<IActionResult> Salvar(Usuario modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid) return View("Formulario", modelo);
        try
        {
            await _servico.SalvarUsuarioAsync(modelo, UsuarioAtual);
            TempData["Sucesso"] = "Usuário salvo com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Formulario", modelo);
        }
    }
}
