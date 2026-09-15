using JcaInventario.Infraestrutura;
using JcaInventario.Repositorios;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using MySqlConnector;
using System.Threading.RateLimiting;
using Dapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;

var construtor = WebApplication.CreateBuilder(args);
construtor.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
construtor.Logging.ClearProviders();
construtor.Logging.AddConsole();
if (construtor.Environment.IsDevelopment()) construtor.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(construtor.Environment.ContentRootPath, "Dados", "ChavesDesenvolvimento"))).UseEphemeralDataProtectionProvider();
else {
    var protecao = construtor.Services.AddDataProtection().SetApplicationName("JcaInventario").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(construtor.Environment.ContentRootPath, "Dados", "Chaves")));
    if (OperatingSystem.IsWindows()) protecao.ProtectKeysWithDpapi();
}
construtor.Services.AddSingleton<Banco>();
construtor.Services.AddScoped<RepositorioInventario>();
construtor.Services.AddScoped<ServicoEquipamentos>();
construtor.Services.AddControllersWithViews().AddJsonOptions(opcoes => opcoes.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
construtor.Services.Configure<ApiBehaviorOptions>(opcoes => opcoes.InvalidModelStateResponseFactory = contexto => new BadRequestObjectResult(new { mensagem = "Confira os campos obrigatórios, formatos e limites: " + string.Join(", ", contexto.ModelState.Where(item => item.Value?.Errors.Count > 0).Select(item => item.Key)) }));
construtor.Services.AddAntiforgery(opcoes => opcoes.HeaderName = "X-CSRF-TOKEN");
construtor.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(opcoes => {
    opcoes.Cookie.Name = "JcaInventario.Sessao";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SameSite = SameSiteMode.Strict;
    opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
    opcoes.Events.OnRedirectToLogin = contexto => { contexto.Response.StatusCode = 401; return Task.CompletedTask; };
    opcoes.Events.OnRedirectToAccessDenied = contexto => { contexto.Response.StatusCode = 403; return Task.CompletedTask; };
    opcoes.Events.OnValidatePrincipal = async contexto => {
        await using var conexao = await contexto.HttpContext.RequestServices.GetRequiredService<Banco>().AbrirAsync();
        var registro = await conexao.QuerySingleOrDefaultAsync("SELECT Nome,Administrador FROM usuarios WHERE Id=@id AND Ativo=1", new { id = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) });
        if (registro == null) { contexto.RejectPrincipal(); return; }
        var identidade = (ClaimsIdentity)contexto.Principal!.Identity!;
        var perfil = Convert.ToBoolean(registro.Administrador) ? "Administrador" : "Operador";
        if (identidade.FindFirst(ClaimTypes.Role)?.Value != perfil) { var papel = identidade.FindFirst(ClaimTypes.Role); if (papel != null) identidade.RemoveClaim(papel); identidade.AddClaim(new Claim(ClaimTypes.Role, perfil)); contexto.ShouldRenew = true; }
    };
});
construtor.Services.AddAuthorization();
construtor.Services.AddRateLimiter(opcoes => {
    opcoes.RejectionStatusCode = 429;
    opcoes.AddPolicy("entrada", contexto => RateLimitPartition.GetFixedWindowLimiter(contexto.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
var aplicacao = construtor.Build();
await InicializadorBanco.InicializarAsync(aplicacao.Services.GetRequiredService<Banco>(), aplicacao.Environment.ContentRootPath);
aplicacao.Use(async (contexto, proximo) => {
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    try {
        await proximo();
    }
    catch (AntiforgeryValidationException) { contexto.Response.StatusCode = 400; await contexto.Response.WriteAsJsonAsync(new { mensagem = "Sessão expirada. Atualize a página e tente novamente." }); }
    catch (ArgumentException erro) { contexto.Response.StatusCode = 400; await contexto.Response.WriteAsJsonAsync(new { mensagem = erro.Message }); }
    catch (KeyNotFoundException) { contexto.Response.StatusCode = 404; await contexto.Response.WriteAsJsonAsync(new { mensagem = "Registro não encontrado." }); }
    catch (MySqlException erro) when (erro.Number == 1062) { contexto.Response.StatusCode = 409; await contexto.Response.WriteAsJsonAsync(new { mensagem = "Já existe um registro com esse patrimônio, número de série, matrícula, nome ou e-mail." }); }
    catch (MySqlException erro) when (erro.Number == 1452) { contexto.Response.StatusCode = 400; await contexto.Response.WriteAsJsonAsync(new { mensagem = "O cadastro relacionado não existe." }); }
    catch (Exception erro) { aplicacao.Logger.LogError(erro, "Erro ao processar solicitação"); contexto.Response.StatusCode = 500; await contexto.Response.WriteAsJsonAsync(new { mensagem = "Não foi possível concluir a operação. Consulte o registro do servidor." }); }
});
aplicacao.UseStaticFiles();
aplicacao.UseAuthentication();
aplicacao.Use(async (contexto, proximo) => {
    if (contexto.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(contexto.Request.Method))
        await contexto.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(contexto);
    await proximo();
});
aplicacao.UseAuthorization();
aplicacao.UseRateLimiter();
aplicacao.MapControllers();
aplicacao.MapControllerRoute("principal", "{controller=Inicio}/{action=Index}/{id?}");
aplicacao.Run();
