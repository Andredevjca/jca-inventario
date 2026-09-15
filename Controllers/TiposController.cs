using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class TiposController : Controller
{
    private readonly IServicoCadastros _servico;

    public TiposController(IServicoCadastros servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Tipos de equipamento";
        return View(Formatador.Lista(await _servico.ListarAsync("tipos")));
    }

    public IActionResult Criar()
    {
        ViewData["Title"] = "Novo tipo";
        return View("Formulario", new Cadastro());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Cadastro modelo)
    {
        return await Salvar(modelo, "Novo tipo");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar tipo";
        var item = Formatador.Lista(await _servico.ListarAsync("tipos")).FirstOrDefault(tipo => Convert.ToInt32(tipo.Id) == id);
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
        return await Salvar(modelo, "Editar tipo");
    }

    private async Task<IActionResult> Salvar(Cadastro modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid) return View("Formulario", modelo);
        try
        {
            await _servico.SalvarCadastroAsync(TipoCadastro.TipoEquipamento, modelo);
            TempData["Sucesso"] = "Tipo salvo com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Formulario", modelo);
        }
    }
}
