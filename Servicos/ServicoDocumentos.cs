using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.ViewModels;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Servicos;

public class ServicoDocumentos(Banco banco, IRepositorioEquipamentos repositorio, ServicoEquipamentos servicoEquipamentos, IWebHostEnvironment ambiente) : IServicoDocumentos
{
    private const int Limite = 3;
    private const long TamanhoMaximo = 5 * 1024 * 1024;

    public async Task<IEnumerable<EquipamentoDocumento>> ListarAsync(int equipamentoId)
    {
        await using var conexao = await banco.AbrirAsync();
        return await repositorio.ListarDocumentosAsync(conexao, equipamentoId);
    }

    public async Task AtualizarAsync(int id, IReadOnlyList<ArquivoEnviado> novos, IReadOnlyList<int> remover, int usuario)
    {
        var arquivos = new List<(byte[] Bytes, string Nome, string Original)>();
        foreach (var item in novos.Where(item => item.Tamanho > 0))
            arquivos.Add((await LerValidarAsync(item), Guid.NewGuid().ToString("N") + ".pdf", NomeOriginal(item.NomeArquivo)));
        if (arquivos.Count > Limite) throw new ArgumentException("Cada equipamento pode ter no máximo 3 notas fiscais.");

        var pasta = Path.Combine(ambiente.ContentRootPath, "Dados", "Documentos");
        Directory.CreateDirectory(pasta);
        var gravados = new List<string>();
        try
        {
            foreach (var arquivo in arquivos)
            {
                await File.WriteAllBytesAsync(Path.Combine(pasta, arquivo.Nome), arquivo.Bytes);
                gravados.Add(arquivo.Nome);
            }

            await using var conexao = await banco.AbrirAsync();
            await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync();
            _ = await repositorio.ObterParaAtualizacaoAsync(conexao, new { Id = id }, transacao) ?? throw new KeyNotFoundException();
            var atuais = (await repositorio.ListarDocumentosAsync(conexao, id, transacao)).ToList();
            var idsRemover = remover.Distinct().ToHashSet();
            var excluidos = atuais.Where(documento => idsRemover.Contains(documento.Id)).ToList();
            if (excluidos.Count != idsRemover.Count) throw new ArgumentException("Nota fiscal inválida.");
            if (atuais.Count - excluidos.Count + arquivos.Count > Limite) throw new ArgumentException("Cada equipamento pode ter no máximo 3 notas fiscais.");

            foreach (var documento in excluidos)
                await repositorio.RemoverDocumentoAsync(conexao, id, documento.Id, transacao);
            foreach (var arquivo in arquivos)
                await repositorio.InserirDocumentoAsync(conexao, new EquipamentoDocumento { EquipamentoId = id, NomeArquivo = arquivo.Nome, NomeOriginal = arquivo.Original }, transacao);

            if (excluidos.Count > 0)
                await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, id, usuario, excluidos.Count == 1 ? "Nota fiscal removida" : "Notas fiscais removidas", string.Join(", ", excluidos.Select(documento => documento.NomeOriginal)), null);
            if (arquivos.Count > 0)
                await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, id, usuario, arquivos.Count == 1 ? "Nota fiscal anexada" : "Notas fiscais anexadas", null, string.Join(", ", arquivos.Select(arquivo => arquivo.Original)));

            await transacao.CommitAsync();
            foreach (var documento in excluidos)
            {
                var caminho = Path.Combine(pasta, Path.GetFileName(documento.NomeArquivo));
                if (File.Exists(caminho)) File.Delete(caminho);
            }
        }
        catch
        {
            foreach (var nome in gravados)
            {
                var caminho = Path.Combine(pasta, nome);
                if (File.Exists(caminho)) File.Delete(caminho);
            }
            throw;
        }
    }

    public async Task<DocumentoDisponivel?> ObterAsync(int equipamentoId, int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var documento = await repositorio.ObterDocumentoAsync(conexao, equipamentoId, id);
        if (documento == null) return null;
        var caminho = Path.Combine(ambiente.ContentRootPath, "Dados", "Documentos", Path.GetFileName(documento.NomeArquivo));
        if (!File.Exists(caminho)) return null;
        return new DocumentoDisponivel(caminho, "application/pdf", documento.NomeOriginal);
    }

    private static string NomeOriginal(string nome)
    {
        var limpo = Path.GetFileName(nome);
        if (string.IsNullOrWhiteSpace(limpo)) return "nota-fiscal.pdf";
        return limpo.Length > 260 ? limpo[..260] : limpo;
    }

    private static async Task<byte[]> LerValidarAsync(ArquivoEnviado item)
    {
        var extensao = Path.GetExtension(item.NomeArquivo).ToLowerInvariant();
        if (extensao != ".pdf" || item.Tamanho == 0 || item.Tamanho > TamanhoMaximo) throw new ArgumentException("Selecione arquivos PDF de até 5 MB.");
        await using var memoria = new MemoryStream();
        await item.Conteudo.CopyToAsync(memoria);
        var bytes = memoria.ToArray();
        if (bytes.Length < 5 || bytes[0] != 0x25 || bytes[1] != 0x50 || bytes[2] != 0x44 || bytes[3] != 0x46) throw new ArgumentException("O conteúdo do arquivo não corresponde a um PDF.");
        return bytes;
    }
}
