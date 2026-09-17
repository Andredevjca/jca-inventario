using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Repositories;
using JcaInventario.Servicos;

namespace JcaInventario.Dependecias;

public static class InjecaoDependencias
{
    public static IServiceCollection AdicionarDependencias(this IServiceCollection services)
    {
        services.AddSingleton<Banco>();
        services.AddScoped<IRepositorioInventario, RepositorioInventario>();
        services.AddScoped<IRepositorioCadastros, RepositorioCadastros>();
        services.AddScoped<IRepositorioEquipamentos, RepositorioEquipamentos>();
        services.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        services.AddScoped<IRepositorioInicializacao, RepositorioInicializacao>();
        services.AddScoped<ServicoEquipamentos>();
        services.AddScoped<IServicoEquipamentos>(provedor => provedor.GetRequiredService<ServicoEquipamentos>());
        services.AddScoped<IServicoCadastros, ServicoCadastros>();
        services.AddScoped<IServicoInventario, ServicoInventario>();
        services.AddScoped<IServicoManutencoes, ServicoManutencoes>();
        services.AddScoped<IServicoFotos, ServicoFotos>();
        services.AddScoped<IServicoDocumentos, ServicoDocumentos>();
        services.AddScoped<IServicoTermos, ServicoTermos>();
        services.AddScoped<IServicoConta, ServicoConta>();
        services.AddScoped<ServicoInicializacao>();
        return services;
    }
}
