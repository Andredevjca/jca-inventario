using Dapper;
using JcaInventario.DTOs;
using JcaInventario.Infraestrutura;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
namespace JcaInventario.Controllers;

[ApiController, Route("api/conta")]
public class ContaController(Banco banco, IAntiforgery antifalsificacao) : ControllerBase
{
    [HttpGet("token")]
    public object Token() => new { token = antifalsificacao.GetAndStoreTokens(HttpContext).RequestToken };
    [HttpPost("entrar"), EnableRateLimiting("entrada")]
    public async Task<IActionResult> Entrar(Entrada entrada)
    {
        await using var conexao = await banco.AbrirAsync();
        var usuario = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM usuarios WHERE Email=@Email AND Ativo=1", entrada);
        if (usuario is null) return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });
        string resumo = usuario.SenhaHash;
        if (!Senhas.Verificar(entrada.Senha, resumo)) return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });
        var identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Convert.ToString(usuario.Id)!), new Claim(ClaimTypes.Name, (string)usuario.Nome), new Claim(ClaimTypes.Role, Convert.ToBoolean(usuario.Administrador) ? "Administrador" : "Operador") }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade), new AuthenticationProperties { IsPersistent = entrada.Lembrar });
        return Ok(new { mensagem = "Acesso autorizado." });
    }
    [Authorize, HttpGet("atual")]
    public object Atual() => new { nome = User.Identity!.Name, administrador = User.IsInRole("Administrador") };
    [Authorize, HttpPost("sair")]
    public async Task<IActionResult> Sair() { await HttpContext.SignOutAsync(); return NoContent(); }
}
