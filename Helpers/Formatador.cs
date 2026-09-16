using System.Globalization;

namespace JcaInventario.Helpers;

public static class Formatador
{
    private static readonly CultureInfo Cultura = new("pt-BR");

    public static string Moeda(decimal? valor)
    {
        return (valor ?? 0).ToString("C", Cultura);
    }

    public static string Data(DateTime? data)
    {
        return data?.ToString("dd/MM/yyyy", Cultura) ?? "—";
    }

    public static string DataHora(DateTime? data)
    {
        return data?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";
    }

    public static string TempoDeEmpresa(DateTime admissao)
    {
        var inicio = admissao.Date;
        var hoje = DateTime.Today;
        if (inicio > hoje) return "Admissão futura";
        if (inicio == hoje) return "Primeiro dia na empresa";

        var mesesTotais = (hoje.Year - inicio.Year) * 12 + hoje.Month - inicio.Month;
        if (inicio.AddMonths(mesesTotais) > hoje) mesesTotais--;
        var anos = mesesTotais / 12;
        var meses = mesesTotais % 12;
        var dias = (hoje - inicio.AddMonths(mesesTotais)).Days;
        var partes = new List<string>();
        if (anos > 0) partes.Add($"{anos} {(anos == 1 ? "ano" : "anos")}");
        if (meses > 0) partes.Add($"{meses} {(meses == 1 ? "mês" : "meses")}");
        if (dias > 0) partes.Add($"{dias} {(dias == 1 ? "dia" : "dias")}");
        return partes.Count <= 2
            ? string.Join(" e ", partes)
            : $"{partes[0]}, {partes[1]} e {partes[2]}";
    }

    public static string Texto(object? valor)
    {
        return string.IsNullOrWhiteSpace(Convert.ToString(valor)) ? "—" : Convert.ToString(valor)!;
    }

    public static IEnumerable<dynamic> Lista(object? origem)
    {
        return origem as IEnumerable<dynamic> ?? [];
    }

    public static string ClasseStatus(string? status)
    {
        return status switch
        {
            "Ativo" => "status-ativo",
            "Conferido" or "Disponível" or "Em uso" or "Encerrado" => "status-pago",
            "Em manutenção" or "Pendente" or "Home Office" => "status-pendente",
            "Divergência" or "Baixado" or "Inativo" => "status-atrasado",
            _ => "status-info"
        };
    }
}
