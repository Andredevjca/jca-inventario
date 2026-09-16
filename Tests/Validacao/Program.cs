using System.Globalization;
using JcaInventario.Configuracoes;
using JcaInventario.Models;
using JcaInventario.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

// Usa os provedores reais do MVC, incluindo Required implícito de strings não anuláveis.
var servicos = new ServiceCollection();
servicos.AddLogging();
servicos.AddControllersWithViews(opcoes => opcoes.ModelMetadataDetailsProviders.Add(new MensagensValidacaoPortugues()));
using var provedor = servicos.BuildServiceProvider();
var metadados = provedor.GetRequiredService<IModelMetadataProvider>();
var validador = provedor.GetRequiredService<IObjectModelValidator>();

ActionContext NovoContexto() => new(new DefaultHttpContext { RequestServices = provedor },
    new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

void Conferir(ActionContext contexto, string campo, string esperado)
{
    var mensagens = contexto.ModelState[campo]?.Errors.Select(erro => erro.ErrorMessage).ToArray() ?? [];
    if (!mensagens.Contains(esperado))
        throw new Exception($"{campo}: esperado '{esperado}', recebido '{string.Join("; ", mensagens)}'.");
    Console.WriteLine($"OK: {esperado}");
}

foreach (var cultura in new[] { "pt-BR", "en-US" })
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultura);
    var contexto = NovoContexto();
    var tipo = metadados.GetMetadataForProperty(typeof(Equipamento), nameof(Equipamento.TipoId));
    var binding = DefaultModelBindingContext.CreateBindingContext(contexto,
        new FormValueProvider(BindingSource.Form, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        { ["TipoId"] = "" }), CultureInfo.CurrentCulture), tipo, null, "TipoId");
    await new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance).BindModelAsync(binding);
    Conferir(contexto, "TipoId", "O campo Tipo é obrigatório.");

    contexto = NovoContexto();
    validador.Validate(contexto, null, "", new Equipamento { Status = null! });
    Conferir(contexto, "TipoId", "Selecione uma opção válida para o campo Tipo.");
    Conferir(contexto, "Marca", "O campo Marca é obrigatório.");
    Conferir(contexto, "Modelo", "O campo Modelo é obrigatório.");
    Conferir(contexto, "Status", "O campo Status é obrigatório.");

    contexto = NovoContexto();
    validador.Validate(contexto, null, "", new Equipamento { TipoId = 1, Marca = new string('a', 101), Modelo = "Teste", ValorAquisicao = -1 });
    Conferir(contexto, "Marca", "O campo Marca deve ter no máximo 100 caracteres.");
    if (!contexto.ModelState["ValorAquisicao"]!.Errors.Single().ErrorMessage.StartsWith("O campo Valor de aquisição deve estar entre "))
        throw new Exception("Mensagem de intervalo não traduzida.");

    contexto = NovoContexto();
    validador.Validate(contexto, null, "", new LoginViewModel());
    Conferir(contexto, "Email", "Informe o e-mail.");
    Conferir(contexto, "Senha", "Informe a senha.");
    contexto = NovoContexto();
    validador.Validate(contexto, null, "", new LoginViewModel { Email = "invalido", Senha = "teste" });
    Conferir(contexto, "Email", "Informe um e-mail válido para o campo E-mail.");

    contexto = NovoContexto();
    validador.Validate(contexto, null, "", new Equipamento { TipoId = 1, Marca = "Marca", Modelo = "Modelo" });
    if (!contexto.ModelState.IsValid) throw new Exception("Equipamento válido foi rejeitado.");
}
Console.WriteLine("Validações conferidas em pt-BR e en-US.");
