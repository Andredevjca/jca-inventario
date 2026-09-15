using JcaInventario.Dependecias;
using JcaInventario.Interfaces.Services;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

var cultura = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var construtor = WebApplication.CreateBuilder(args);
construtor.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();

if (construtor.Environment.IsDevelopment())
    construtor.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(construtor.Environment.ContentRootPath, "Dados", "ChavesDesenvolvimento"))).UseEphemeralDataProtectionProvider();
else
{
    var protecao = construtor.Services.AddDataProtection().SetApplicationName("JcaInventario").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(construtor.Environment.ContentRootPath, "Dados", "Chaves")));
    if (OperatingSystem.IsWindows()) protecao.ProtectKeysWithDpapi();
}

construtor.Services.AddControllersWithViews();
construtor.Services.AddHttpContextAccessor();
construtor.Services.AdicionarDependencias();

construtor.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Conta/Entrar";
        opcoes.LogoutPath = "/Conta/Sair";
        opcoes.AccessDeniedPath = "/Conta/Entrar";
        opcoes.Cookie.Name = "JcaInventario.Sessao";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
        opcoes.Events.OnValidatePrincipal = async contexto =>
        {
            var servico = contexto.HttpContext.RequestServices.GetRequiredService<IServicoConta>();
            if (!int.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                contexto.RejectPrincipal();
                return;
            }
            var registro = await servico.ObterAtivoAsync(id);
            if (registro == null)
            {
                contexto.RejectPrincipal();
                return;
            }
            var identidade = (ClaimsIdentity)contexto.Principal!.Identity!;
            var perfil = registro.Administrador ? "Administrador" : "Operador";
            if (identidade.FindFirst(ClaimTypes.Role)?.Value != perfil)
            {
                var papel = identidade.FindFirst(ClaimTypes.Role);
                if (papel != null) identidade.RemoveClaim(papel);
                identidade.AddClaim(new Claim(ClaimTypes.Role, perfil));
                contexto.ShouldRenew = true;
            }
        };
    });

construtor.Services.AddAuthorization(opcoes =>
{
    opcoes.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

construtor.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = 429;
    opcoes.AddPolicy("entrada", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var aplicacao = construtor.Build();
await using (var escopo = aplicacao.Services.CreateAsyncScope())
    await escopo.ServiceProvider.GetRequiredService<ServicoInicializacao>().InicializarAsync(aplicacao.Environment.ContentRootPath);

if (!aplicacao.Environment.IsDevelopment())
{
    aplicacao.UseExceptionHandler("/Home/Error");
    aplicacao.UseHsts();
}

aplicacao.Use(async (contexto, proximo) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    await proximo();
});

aplicacao.UseStaticFiles();
aplicacao.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});
aplicacao.UseRouting();
aplicacao.UseAuthentication();
aplicacao.UseAuthorization();
aplicacao.UseRateLimiter();
aplicacao.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
aplicacao.Run();
