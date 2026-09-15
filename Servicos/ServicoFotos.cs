using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Repositories;
using JcaInventario.ViewModels;

namespace JcaInventario.Servicos;

public class ServicoFotos(Banco banco, IRepositorioEquipamentos repositorio, ServicoEquipamentos servicoEquipamentos, IWebHostEnvironment ambiente) : IServicoFotos
{
    public async Task<string> SalvarAsync(int id, string nomeArquivo, long tamanho, Stream conteudo, int usuario)
    {
        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extensao) || tamanho == 0 || tamanho > 5 * 1024 * 1024) throw new ArgumentException("Selecione uma imagem JPG, PNG ou WebP de até 5 MB.");
        await using var memoria = new MemoryStream(); await conteudo.CopyToAsync(memoria); var bytes = memoria.ToArray();
        var valido = extensao is ".jpg" or ".jpeg" ? bytes.Length > 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 : extensao == ".png" ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) : bytes.Length > 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";
        if (!valido) throw new ArgumentException("O conteúdo do arquivo não corresponde ao formato da imagem.");
        var pasta = Path.Combine(ambiente.ContentRootPath, "Dados", "Fotos"); Directory.CreateDirectory(pasta);
        var nome = Guid.NewGuid().ToString("N") + extensao;
        await System.IO.File.WriteAllBytesAsync(Path.Combine(pasta, nome), bytes);
        try { await AlterarFotoAsync(id, nome, usuario); } catch { System.IO.File.Delete(Path.Combine(pasta, nome)); throw; }
        return nome;
    }
    public Task RemoverAsync(int id, int usuario) => AlterarFotoAsync(id, null, usuario);
    private async Task AlterarFotoAsync(int id, string? nome, int usuario)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = await conexao.BeginTransactionAsync();
        var equipamento = await repositorio.ObterParaAtualizacaoAsync(conexao, new { id }, transacao) ?? throw new KeyNotFoundException();
        await repositorio.AtualizarFotoAsync(conexao, id, nome, transacao);
        await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, id, usuario, nome == null ? "Foto removida" : "Foto alterada", equipamento.Foto, nome);
        await transacao.CommitAsync();
        if (equipamento.Foto != null) System.IO.File.Delete(Path.Combine(ambiente.ContentRootPath, "Dados", "Fotos", Path.GetFileName(equipamento.Foto)));
    }
    public async Task<FotoDisponivel?> ObterAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var nome = await repositorio.ObterFotoAsync(conexao, id);
        if (nome == null) return null;
        var caminho = Path.Combine(ambiente.ContentRootPath, "Dados", "Fotos", Path.GetFileName(nome));
        if (!System.IO.File.Exists(caminho)) return null;
        return new FotoDisponivel(caminho, Path.GetExtension(nome) == ".png" ? "image/png" : Path.GetExtension(nome) == ".webp" ? "image/webp" : "image/jpeg");
    }
}
