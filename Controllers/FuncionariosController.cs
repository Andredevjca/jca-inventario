using JcaInventario.Helpers;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using Microsoft.AspNetCore.Mvc;

namespace JcaInventario.Controllers;

public class FuncionariosController : PainelController
{
    private readonly IServicoCadastros _servico;

    public FuncionariosController(IServicoCadastros servico)
    {
        _servico = servico;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Funcionários";
        return View(Formatador.Lista(await _servico.ListarAsync("funcionarios")));
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        ViewData["Title"] = "Detalhes do funcionário";
        try
        {
            return View(await _servico.ObterFuncionarioAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Criar()
    {
        ViewData["Title"] = "Novo funcionário";
        ViewBag.Funcoes = Formatador.Lista(await _servico.ListarAsync("funcoes"));
        ViewBag.Setores = Formatador.Lista(await _servico.ListarAsync("setores"));
        return View("Formulario", new Funcionario());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Funcionario modelo)
    {
        return await Salvar(modelo, "Novo funcionário");
    }

    public async Task<IActionResult> Editar(int id)
    {
        ViewData["Title"] = "Editar funcionário";
        try
        {
            dynamic dados = await _servico.ObterFuncionarioAsync(id);
            ViewBag.Funcoes = Formatador.Lista(await _servico.ListarAsync("funcoes"));
            ViewBag.Setores = Formatador.Lista(await _servico.ListarAsync("setores"));
            var item = dados.funcionario;
            return View("Formulario", new Funcionario
            {
                Id = Convert.ToInt32(item.Id),
                Nome = Convert.ToString(item.Nome) ?? "",
                Email = Convert.ToString(item.Email),
                Telefone = Convert.ToString(item.Telefone),
                Matricula = Convert.ToString(item.Matricula),
                FuncaoId = (int?)item.FuncaoId,
                Endereco = Convert.ToString(item.Endereco),
                Numero = Convert.ToString(item.Numero),
                Cep = Convert.ToString(item.Cep),
                Bairro = Convert.ToString(item.Bairro),
                Cidade = Convert.ToString(item.Cidade),
                Uf = Convert.ToString(item.Uf),
                Complemento = Convert.ToString(item.Complemento),
                DataNascimento = (DateTime?)item.DataNascimento,
                DataAdmissao = (DateTime?)item.DataAdmissao,
                Cargo = Convert.ToString(item.Cargo),
                SetorId = Convert.ToInt32(item.SetorId),
                TipoTrabalho = Convert.ToString(item.TipoTrabalho) ?? "Presencial",
                Ativo = Convert.ToBoolean(item.Ativo),
                Observacoes = Convert.ToString(item.Observacoes)
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Funcionario modelo)
    {
        modelo.Id = id;
        return await Salvar(modelo, "Editar funcionário");
    }

    private async Task<IActionResult> Salvar(Funcionario modelo, string titulo)
    {
        ViewData["Title"] = titulo;
        if (!ModelState.IsValid)
        {
            ViewBag.Funcoes = Formatador.Lista(await _servico.ListarAsync("funcoes"));
            ViewBag.Setores = Formatador.Lista(await _servico.ListarAsync("setores"));
            return View("Formulario", modelo);
        }

        try
        {
            dynamic resultado = await _servico.SalvarFuncionarioAsync(modelo, UsuarioAtual);
            TempData["Sucesso"] = "Funcionário salvo com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id = (int)resultado.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Funcoes = Formatador.Lista(await _servico.ListarAsync("funcoes"));
            ViewBag.Setores = Formatador.Lista(await _servico.ListarAsync("setores"));
            return View("Formulario", modelo);
        }
    }
}
