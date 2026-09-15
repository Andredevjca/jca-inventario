using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class ManutencoesController : PainelController
{
    private readonly IServicoInventario _inventario;
    private readonly IServicoManutencoes _servico;
    private readonly IServicoEquipamentos _equipamentos;

    public ManutencoesController(IServicoInventario inventario, IServicoManutencoes servico, IServicoEquipamentos equipamentos)
    {
        _inventario = inventario;
        _servico = servico;
        _equipamentos = equipamentos;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Manutenções";
        return View(Formatador.Lista(await _inventario.ListarManutencoesAsync()));
    }

    public async Task<IActionResult> Criar()
    {
        ViewData["Title"] = "Nova manutenção";
        ViewBag.Equipamentos = Formatador.Lista(await _equipamentos.ListarAsync());
        return View("Formulario", new Manutencao { DataEntrada = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Manutencao modelo)
    {
        return await Salvar(modelo, "Nova manutenção");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Atualizar manutenção";
        var item = Formatador.Lista(await _inventario.ListarManutencoesAsync()).FirstOrDefault(manutencao => Convert.ToInt32(manutencao.Id) == id);
        if (item == null) return NotFound();
        ViewBag.Equipamentos = Formatador.Lista(await _equipamentos.ListarAsync());
        return View("Formulario", new Manutencao
        {
            Id = Convert.ToInt32(item.Id),
            EquipamentoId = Convert.ToInt32(item.EquipamentoId),
            DataEntrada = Convert.ToDateTime(item.DataEntrada),
            DataSaida = item.DataSaida as DateTime?,
            Tipo = Convert.ToString(item.Tipo) ?? "",
            Problema = Convert.ToString(item.Problema) ?? "",
            Solucao = Convert.ToString(item.Solucao),
            Valor = Convert.ToDecimal(item.Valor ?? 0),
            Responsavel = Convert.ToString(item.Responsavel) ?? "",
            Observacoes = Convert.ToString(item.Observacoes)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Manutencao modelo)
    {
        modelo.Id = id;
        return await Salvar(modelo, "Atualizar manutenção");
    }

    private async Task<IActionResult> Salvar(Manutencao modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid)
        {
            ViewBag.Equipamentos = Formatador.Lista(await _equipamentos.ListarAsync());
            return View("Formulario", modelo);
        }

        try
        {
            await _servico.SalvarAsync(modelo, UsuarioAtual);
            TempData["Sucesso"] = "Manutenção salva com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Equipamentos = Formatador.Lista(await _equipamentos.ListarAsync());
            return View("Formulario", modelo);
        }
    }
}
