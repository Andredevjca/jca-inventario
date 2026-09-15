using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class SetoresController : Controller
{
    private readonly IServicoCadastros _servico;

    public SetoresController(IServicoCadastros servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Setores";
        return View(Formatador.Lista(await _servico.ListarAsync("setores")));
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        ViewData["Title"] = "Detalhes do setor";
        try
        {
            return View(await _servico.ObterSetorAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public IActionResult Criar()
    {
        ViewData["Title"] = "Novo setor";
        return View("Formulario", new Cadastro());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Cadastro modelo)
    {
        return await Salvar(modelo, "Novo setor");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar setor";
        var item = Formatador.Lista(await _servico.ListarAsync("setores")).FirstOrDefault(setor => Convert.ToInt32(setor.Id) == id);
        if (item == null) return NotFound();
        return View("Formulario", new Cadastro
        {
            Id = Convert.ToInt32(item.Id),
            Nome = Convert.ToString(item.Nome) ?? "",
            Ativo = Convert.ToBoolean(item.Ativo),
            Observacoes = Convert.ToString(item.Observacoes)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Cadastro modelo)
    {
        modelo.Id = id;
        return await Salvar(modelo, "Editar setor");
    }

    private async Task<IActionResult> Salvar(Cadastro modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid) return View("Formulario", modelo);
        try
        {
            await _servico.SalvarCadastroAsync(TipoCadastro.Setor, modelo);
            TempData["Sucesso"] = "Setor salvo com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Formulario", modelo);
        }
    }
}
