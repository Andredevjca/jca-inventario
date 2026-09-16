using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class FuncoesController : Controller
{
    private readonly IServicoCadastros _servico;

    public FuncoesController(IServicoCadastros servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Funções";
        return View(Formatador.Lista(await _servico.ListarAsync("funcoes")));
    }

    public IActionResult Criar()
    {
        ViewData["Title"] = "Nova função";
        return View("Formulario", new Cadastro());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Cadastro modelo)
    {
        return await Salvar(modelo, "Nova função");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar função";
        var item = Formatador.Lista(await _servico.ListarAsync("funcoes")).FirstOrDefault(tipo => Convert.ToInt32(tipo.Id) == id);
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
        return await Salvar(modelo, "Editar função");
    }

    private async Task<IActionResult> Salvar(Cadastro modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid) return View("Formulario", modelo);
        try
        {
            await _servico.SalvarCadastroAsync(TipoCadastro.Funcao, modelo);
            TempData["Sucesso"] = "Função salva com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Formulario", modelo);
        }
    }
}
