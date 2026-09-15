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
