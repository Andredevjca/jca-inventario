using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class InventariosController : PainelController
{
    private readonly IServicoInventario _servico;

    public InventariosController(IServicoInventario servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Inventários";
        return View(Formatador.Lista(await _servico.ListarInventariosAsync()));
    }

    public IActionResult Criar()
    {
        ViewData["Title"] = "Novo inventário";
        return View(new NovoInventarioViewModel { Nome = $"Inventário {DateTime.Now:MMMM yyyy}" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(NovoInventarioViewModel modelo)
    {
        ViewData["Title"] = "Novo inventário";
        if (!ModelState.IsValid) return View(modelo);
        try
        {
            dynamic resultado = await _servico.CriarInventarioAsync(modelo, UsuarioAtual);
            TempData["Sucesso"] = "Inventário criado.";
            return RedirectToAction(nameof(Detalhes), new { id = (int)resultado.id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(modelo);
        }
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        ViewData["Title"] = "Conferência";
        try
        {
            return View(await _servico.ObterConferenciasAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Conferir(int id, int equipamentoId, ConferenciaViewModel modelo)
    {
        try
        {
            await _servico.ConferirAsync(id, equipamentoId, modelo, UsuarioAtual);
            TempData["Sucesso"] = "Conferência registrada.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Encerrar(int id)
    {
        try
        {
            await _servico.EncerrarAsync(id);
            TempData["Sucesso"] = "Inventário encerrado.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
