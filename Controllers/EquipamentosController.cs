using JcaInventario.DTOs;
using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class EquipamentosController : PainelController
{
    private readonly IServicoEquipamentos _servico;
    private readonly IServicoCadastros _cadastros;
    private readonly IServicoFotos _fotos;
    private readonly IServicoTermos _termos;

    public EquipamentosController(IServicoEquipamentos servico, IServicoCadastros cadastros, IServicoFotos fotos, IServicoTermos termos)
    {
        _servico = servico;
        _cadastros = cadastros;
        _fotos = fotos;
        _termos = termos;
    }

    public async Task<IActionResult> Index(string? busca, string? status, string? localizacao)
    {
        ViewData["Title"] = "Equipamentos";
        ViewBag.Busca = busca;
        ViewBag.Status = status;
        ViewBag.Localizacao = localizacao;
        ViewBag.StatusOpcoes = OpcoesInventario.Status;
        ViewBag.Localizacoes = OpcoesInventario.Localizacoes;

        var itens = Formatador.Lista(await _servico.ListarAsync()).Where(item =>
            (string.IsNullOrWhiteSpace(busca) || Convert.ToString(item.NumeroPatrimonio)?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true
                || Convert.ToString(item.Modelo)?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true
                || Convert.ToString(item.Marca)?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true
                || Convert.ToString(item.Responsavel)?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true)
            && (string.IsNullOrWhiteSpace(status) || Convert.ToString(item.Status) == status)
            && (string.IsNullOrWhiteSpace(localizacao) || Convert.ToString(item.Localizacao) == localizacao));
        return View(itens);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        ViewData["Title"] = "Detalhes do equipamento";
        try
        {
            return View(await _servico.ObterDetalhesAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Criar()
    {
        ViewData["Title"] = "Novo equipamento";
        await CarregarOpcoes();
        return View("Formulario", new Equipamento());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Criar(Equipamento equipamento, IFormFile? arquivo)
    {
        ViewData["Title"] = "Novo equipamento";
        return await Salvar(equipamento, arquivo, false);
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar equipamento";
        try
        {
            dynamic dados = await _servico.ObterDetalhesAsync(id);
            await CarregarOpcoes();
            return View("Formulario", Mapear(dados.equipamento));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Editar(int id, Equipamento equipamento, IFormFile? arquivo, bool removerFoto = false)
    {
        ViewData["Title"] = "Editar equipamento";
        equipamento.Id = id;
        return await Salvar(equipamento, arquivo, removerFoto);
    }

    public async Task<IActionResult> Movimentar(int id)
    {
        ViewData["Title"] = "Movimentar equipamento";
        try
        {
            dynamic dados = await _servico.ObterDetalhesAsync(id);
            ViewBag.Equipamento = dados.equipamento;
            ViewBag.Funcionarios = Formatador.Lista(await _cadastros.ListarAsync("funcionarios"));
            ViewBag.Movimentacoes = OpcoesInventario.Movimentacoes;
            ViewBag.Status = OpcoesInventario.Status.Where(item => item != "Em manutenção");
            ViewBag.Localizacoes = OpcoesInventario.Localizacoes.Where(item => item != "Manutenção");
            return View(new MovimentacaoViewModel
            {
                ResponsavelId = dados.equipamento.ResponsavelId,
                Status = dados.equipamento.Status,
                Localizacao = dados.equipamento.Localizacao,
                Tipo = dados.equipamento.ResponsavelId == null ? "Entrega" : "Transferência"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Movimentar(int id, MovimentacaoViewModel modelo)
    {
        try
        {
            await _servico.MovimentarAsync(id, modelo, UsuarioAtual);
            TempData["Sucesso"] = "Movimentação registrada.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            dynamic dados = await _servico.ObterDetalhesAsync(id);
            ViewBag.Equipamento = dados.equipamento;
            ViewBag.Funcionarios = Formatador.Lista(await _cadastros.ListarAsync("funcionarios"));
            ViewBag.Movimentacoes = OpcoesInventario.Movimentacoes;
            ViewBag.Status = OpcoesInventario.Status.Where(item => item != "Em manutenção");
            ViewBag.Localizacoes = OpcoesInventario.Localizacoes.Where(item => item != "Manutenção");
            return View(modelo);
        }
    }

    public async Task<IActionResult> Termo(int id)
    {
        var termo = await _termos.GerarAsync(id);
        return File(termo.Conteudo, termo.TipoConteudo, termo.Nome);
    }

    public async Task<IActionResult> Foto(int id)
    {
        var foto = await _fotos.ObterAsync(id);
        return foto == null ? NotFound() : PhysicalFile(foto.Caminho, foto.TipoConteudo);
    }

    private async Task<IActionResult> Salvar(Equipamento modelo, IFormFile? arquivo, bool removerFoto)
    {
        if (!ModelState.IsValid)
        {
            await CarregarOpcoes();
            return View("Formulario", modelo);
        }

        try
        {
            var id = await _servico.SalvarAsync(modelo, UsuarioAtual);
            if (arquivo is { Length: > 0 })
            {
                await using var conteudo = arquivo.OpenReadStream();
                await _fotos.SalvarAsync(id, arquivo.FileName, arquivo.Length, conteudo, UsuarioAtual);
            }
            else if (removerFoto)
                await _fotos.RemoverAsync(id, UsuarioAtual);

            TempData["Sucesso"] = "Equipamento salvo com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CarregarOpcoes();
            return View("Formulario", modelo);
        }
    }

    private async Task CarregarOpcoes()
    {
        ViewBag.Tipos = Formatador.Lista(await _cadastros.ListarAsync("tipos"));
        ViewBag.Funcionarios = Formatador.Lista(await _cadastros.ListarAsync("funcionarios"));
        ViewBag.Status = OpcoesInventario.Status;
        ViewBag.Localizacoes = OpcoesInventario.Localizacoes;
    }

    private static Equipamento Mapear(dynamic item) => new()
    {
        Id = Convert.ToInt32(item.Id),
        TipoId = Convert.ToInt32(item.TipoId),
        Marca = Convert.ToString(item.Marca) ?? "",
        Modelo = Convert.ToString(item.Modelo) ?? "",
        NumeroPatrimonio = Convert.ToString(item.NumeroPatrimonio),
        NumeroSerie = Convert.ToString(item.NumeroSerie),
        DataAquisicao = item.DataAquisicao as DateTime?,
        ValorAquisicao = Convert.ToDecimal(item.ValorAquisicao ?? 0),
        Status = Convert.ToString(item.Status) ?? "Em estoque",
        Localizacao = Convert.ToString(item.Localizacao) ?? "Estoque",
        ResponsavelId = item.ResponsavelId == null ? null : Convert.ToInt32(item.ResponsavelId),
        Observacoes = Convert.ToString(item.Observacoes),
        Processador = Convert.ToString(item.Processador),
        MemoriaRam = Convert.ToString(item.MemoriaRam),
        Armazenamento = Convert.ToString(item.Armazenamento),
        SistemaOperacional = Convert.ToString(item.SistemaOperacional),
        Foto = Convert.ToString(item.Foto)
    };
}
