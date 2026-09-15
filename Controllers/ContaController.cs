using JcaInventario.Interfaces.Services;
using JcaInventario.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace JcaInventario.Controllers;

public class ContaController : Controller
{
    private readonly IServicoConta _autenticacao;

    public ContaController(IServicoConta autenticacao)
    {
        _autenticacao = autenticacao;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Entrar(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("entrada")]
    public async Task<IActionResult> Entrar(LoginViewModel modelo, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var usuario = await _autenticacao.AutenticarAsync(modelo);

        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(modelo);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Administrador ? "Administrador" : "Operador")
        };

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var propriedades = new AuthenticationProperties
        {
            IsPersistent = modelo.Lembrar,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(modelo.Lembrar ? 72 : 8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade), propriedades);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Entrar));
    }
}
