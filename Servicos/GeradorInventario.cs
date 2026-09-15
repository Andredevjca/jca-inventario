using System.Globalization;
using System.Text;
using JcaInventario.Models;

namespace JcaInventario.Servicos;

public static class GeradorInventario
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    private sealed record Pagina(string Secao, List<string> Linhas);
    private static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Não informado" : valor;

    private static IEnumerable<string> Quebrar(string texto)
    {
        foreach (var linha in texto.Replace("\r", "").Replace("\t", " ").Split('\n'))
        {
            var restante = linha;
            while (restante.Length > 82)
            {
                var corte = restante.LastIndexOf(' ', 82);
                if (corte < 1) corte = 82;
                yield return restante[..corte];
                restante = restante[corte..].TrimStart();
            }
            yield return restante;
        }
    }

    public static byte[] Gerar(RelatorioInventario relatorio)
    {
        var paginas = new List<Pagina>();
        void Adicionar(string secao, IEnumerable<string> linhas)
        {
            foreach (var bloco in linhas.SelectMany(Quebrar).Chunk(40))
                paginas.Add(new Pagina(secao, bloco.ToList()));
        }
        var grupos = relatorio.Itens.GroupBy(i => string.IsNullOrWhiteSpace(i.Setor) ? "Sem setor" : i.Setor)
            .OrderBy(g => g.Key, StringComparer.Create(Cultura, true)).ToList();
        var resumo = new List<string>
        {
            $"Total geral: {relatorio.Itens.Count} equipamentos",
            $"Conferidos: {relatorio.Itens.Count(i => i.Situacao == "Conferido")}",
            $"Pendentes: {relatorio.Itens.Count(i => i.Situacao == "Pendente")}",
            $"Divergências: {relatorio.Itens.Count(i => i.Situacao == "Divergência")}",
            "", "TOTAIS POR SETOR", ""
        };
        foreach (var grupo in grupos) resumo.Add($"{grupo.Key}: {grupo.Count()} equipamentos");
        if (relatorio.Itens.Count == 0) resumo.Add("Este inventário não possui equipamentos.");
        resumo.AddRange(["", "DADOS HISTÓRICOS",
            "Equipamentos, responsáveis e setores são preservados na criação do inventário.",
            "O resultado da conferência pode ser atualizado enquanto ele estiver aberto."]);
        if (relatorio.Itens.Any(i => !i.CapturadoNaCriacao))
            resumo.AddRange(["", "ATENÇÃO: este inventário contém registros anteriores ao histórico detalhado.",
                "Esses dados foram capturados na atualização do sistema, não na criação.",
                "A data da captura está identificada em cada equipamento legado."]);
        Adicionar("Resumo do inventário", resumo);
        foreach (var grupo in grupos)
        {
            var linhas = new List<string> { $"Total do setor: {grupo.Count()} equipamentos", "" };
            foreach (var item in grupo.OrderBy(i => i.NumeroPatrimonio).ThenBy(i => i.EquipamentoId))
            {
                var bloco = new List<string> {
                    $"EQUIPAMENTO #{item.EquipamentoId} | Patrimônio: {Texto(item.NumeroPatrimonio)}",
                    $"Tipo: {Texto(item.Tipo)} | Marca/modelo: {Texto(item.Marca)} {Texto(item.Modelo)}",
                    $"Número de série: {Texto(item.NumeroSerie)}",
                    $"Responsável: {Texto(item.Responsavel)}",
                    $"Localização: {Texto(item.Localizacao)} | Status: {Texto(item.Status)}",
                    $"Conferência: {item.Situacao}",
                    $"Conferido por: {Texto(item.Usuario)} | Data: {item.Data?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "Não conferido"}",
                    $"Observações: {Texto(item.Observacao)}"
                };
                if (!item.CapturadoNaCriacao)
                    bloco.Add($"Registro legado - dados capturados em {item.CapturadoEm.ToString("dd/MM/yyyy HH:mm", Cultura)}.");
                bloco.Add("");
                var detalhe = bloco.SelectMany(Quebrar).ToList();
                if (linhas.Count + detalhe.Count > 40 && linhas.Count > 2)
                {
                    paginas.Add(new Pagina(grupo.Key, linhas));
                    linhas = [];
                }
                foreach (var linha in detalhe)
                {
                    if (linhas.Count == 40)
                    {
                        paginas.Add(new Pagina(grupo.Key, linhas));
                        linhas = [$"EQUIPAMENTO #{item.EquipamentoId} - continuação", ""];
                    }
                    linhas.Add(linha);
                }
            }
            if (linhas.Count > 0) paginas.Add(new Pagina(grupo.Key, linhas));
        }
        return Renderizar(relatorio, paginas);
    }

    private static byte[] Renderizar(RelatorioInventario relatorio, List<Pagina> paginas)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);
        string Escapar(string valor) => valor.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        string Escrever(string valor, int x, int y, string fonte = "F1", int tamanho = 10)
            => $"BT /{fonte} {tamanho} Tf {x} {y} Td ({Escapar(valor)}) Tj ET\n";
        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Count {paginas.Count} /Kids [{string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{5 + i * 2} 0 R"))}] >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>"
        };
        for (var i = 0; i < paginas.Count; i++)
        {
            var pagina = paginas[i];
            var numero = objetos.Count + 1;
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {numero + 1} 0 R >>");
            var conteudo = new StringBuilder("0.08 0.13 0.20 rg 0 772 595 70 re f\n1 1 1 rg\n");
            conteudo.Append(Escrever("JCA SOLUÇÕES | INVENTÁRIO POR SETOR", 48, 802, "F2", 13));
            conteudo.Append("0.13 0.23 0.44 rg\n");
            var y = 748;
            foreach (var linha in Quebrar(relatorio.Nome))
            {
                conteudo.Append(Escrever(linha, 48, y, "F2")); y -= 14;
            }
            conteudo.Append(Escrever($"#{relatorio.Id} | {relatorio.Data.ToString("dd/MM/yyyy HH:mm", Cultura)} | {(relatorio.Encerrado ? "Encerrado" : "Em andamento")}", 48, y));
            y -= 24;
            foreach (var linha in Quebrar(pagina.Secao))
            {
                conteudo.Append(Escrever(linha, 48, y, "F2")); y -= 14;
            }
            y -= 8;
            conteudo.Append("0.12 0.16 0.22 rg\n");
            foreach (var linha in pagina.Linhas)
            {
                conteudo.Append(Escrever(linha, 48, y)); y -= 14;
            }
            conteudo.Append("0.8 0.84 0.9 RG 48 48 m 547 48 l S\n");
            conteudo.Append(Escrever($"Inventário #{relatorio.Id} | Página {i + 1} de {paginas.Count}", 48, 30, tamanho: 9));
            var stream = conteudo.ToString();
            objetos.Add($"<< /Length {encoding.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }
        using var resultado = new MemoryStream();
        void Gravar(string valor) => resultado.Write(encoding.GetBytes(valor));
        Gravar("%PDF-1.4\n");
        var posicoes = new List<long>();
        for (var i = 0; i < objetos.Count; i++)
        {
            posicoes.Add(resultado.Position); Gravar($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }
        var inicio = resultado.Position;
        Gravar($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var posicao in posicoes) Gravar($"{posicao:0000000000} 00000 n \n");
        Gravar($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{inicio}\n%%EOF");
        return resultado.ToArray();
    }
}
