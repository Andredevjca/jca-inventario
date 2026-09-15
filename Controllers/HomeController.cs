using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class HomeController : Controller
{
    private readonly IServicoInventario _dashboard;

    public HomeController(IServicoInventario dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard";
        try
        {
            return View(await _dashboard.ObterDashboardAsync());
        }
        catch (Exception)
        {
            ViewBag.ErroBanco = "Não foi possível carregar o dashboard. Verifique a conexão com o banco.";
            return View(new
            {
                totais = new { Total = 0, EmUso = 0, EmEstoque = 0, EmManutencao = 0, HomeOffice = 0, Baixados = 0, SemResponsavel = 0, SemPatrimonio = 0, SemSerie = 0 },
                porSetor = Array.Empty<object>(),
                porLocalizacao = Array.Empty<object>(),
                porStatus = Array.Empty<object>(),
                pendentes = 0,
                recentes = Array.Empty<object>()
            });
        }
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
