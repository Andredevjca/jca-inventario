using System.Text;
namespace JcaInventario.Servicos;

// PDF textual paginado, com fonte padrão e codificação ocidental para acentos em português.
public static class GeradorTermo
{
    public static byte[] Gerar(IEnumerable<string> linhas)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var codificacao = Encoding.GetEncoding(1252);
        var textos = new List<string>();
        foreach (var linha in linhas)
        {
            var restante = linha.Replace("\r", "").Replace("\n", " ");
            while (restante.Length > 85) { var corte = restante.LastIndexOf(' ', 85); if (corte < 1) corte = 85; textos.Add(restante[..corte]); restante = restante[corte..].TrimStart(); }
            textos.Add(restante);
        }
        var paginas = textos.Chunk(43).ToArray();
        var objetos = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", $"<< /Type /Pages /Count {paginas.Length} /Kids [{string.Join(" ", Enumerable.Range(0, paginas.Length).Select(indice => $"{4 + indice * 2} 0 R"))}] >>", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" };
        foreach (var pagina in paginas)
        {
            var numero = objetos.Count + 1;
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {numero + 1} 0 R >>");
            var conteudo = "BT /F1 11 Tf 16 TL 50 790 Td\n" + string.Join("\n", pagina.Select(linha => "(" + linha.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)") + ") Tj T*")) + "\nET";
            objetos.Add($"<< /Length {codificacao.GetByteCount(conteudo)} >>\nstream\n{conteudo}\nendstream");
        }
        using var resultado = new MemoryStream();
        void Escrever(string texto) => resultado.Write(codificacao.GetBytes(texto));
        Escrever("%PDF-1.4\n"); var posicoes = new List<long>();
        for (var indice = 0; indice < objetos.Count; indice++) { posicoes.Add(resultado.Position); Escrever($"{indice + 1} 0 obj\n{objetos[indice]}\nendobj\n"); }
        var inicio = resultado.Position; Escrever($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var posicao in posicoes) Escrever($"{posicao:0000000000} 00000 n \n");
        Escrever($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{inicio}\n%%EOF");
        return resultado.ToArray();
    }
}
