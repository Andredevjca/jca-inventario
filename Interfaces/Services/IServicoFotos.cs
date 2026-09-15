using JcaInventario.ViewModels;

namespace JcaInventario.Interfaces.Services;

public interface IServicoFotos
{
    Task<string> SalvarAsync(int id, string nomeArquivo, long tamanho, Stream conteudo, int usuario);
    Task RemoverAsync(int id, int usuario);
    Task<FotoDisponivel?> ObterAsync(int id);
}
