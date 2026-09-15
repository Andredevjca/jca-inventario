using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class RelatoriosController : Controller
{
    private readonly IServicoEquipamentos _servico;

    public RelatoriosController(IServicoEquipamentos servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index(string? status, string? localizacao)
    {
        ViewData["Title"] = "Relatórios";
        ViewBag.Status = status;
        ViewBag.Localizacao = localizacao;
        var itens = Formatador.Lista(await _servico.ListarAsync()).Where(item =>
            (string.IsNullOrWhiteSpace(status) || Convert.ToString(item.Status) == status)
            && (string.IsNullOrWhiteSpace(localizacao) || Convert.ToString(item.Localizacao) == localizacao)).ToList();
        ViewBag.Total = itens.Count;
        ViewBag.Valor = itens.Sum(item => Convert.ToDecimal((object?)item.ValorAquisicao ?? 0m));
        return View(itens);
    }
}
