using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class MovimentacoesController : Controller
{
    private readonly IServicoInventario _servico;
    private readonly IServicoEquipamentos _equipamentos;

    public MovimentacoesController(IServicoInventario servico, IServicoEquipamentos equipamentos)
    {
        _servico = servico;
        _equipamentos = equipamentos;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Movimentações";
        var ultimas = Formatador.Lista(await _servico.ListarMovimentacoesAsync())
            .OrderByDescending(item => (DateTime?)item.Data)
            .ThenByDescending(item => (int)item.Id)
            .GroupBy(item => (int)item.EquipamentoId)
            .Select(grupo => grupo.First())
            .ToList();
        return View(ultimas);
    }

    public async Task<IActionResult> Historico(int id)
    {
        ViewData["Title"] = "Histórico de movimentações";
        try
        {
            return View(await _equipamentos.ObterDetalhesAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
