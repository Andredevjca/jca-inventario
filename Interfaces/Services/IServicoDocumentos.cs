using JcaInventario.Models;
using JcaInventario.ViewModels;

namespace JcaInventario.Interfaces.Services;

public interface IServicoDocumentos
{
    Task<IEnumerable<EquipamentoDocumento>> ListarAsync(int equipamentoId);
    Task AtualizarAsync(int id, IReadOnlyList<ArquivoEnviado> novos, IReadOnlyList<int> remover, int usuario);
    Task<DocumentoDisponivel?> ObterAsync(int equipamentoId, int id);
}
