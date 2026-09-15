using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class MovimentacoesController : Controller
{
    private readonly IServicoInventario _servico;

    public MovimentacoesController(IServicoInventario servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Movimentações";
        return View(Formatador.Lista(await _servico.ListarMovimentacoesAsync()));
    }
}
