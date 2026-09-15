using System.ComponentModel.DataAnnotations;

namespace JcaInventario.ViewModels;

public class MovimentacaoViewModel
{
    public int? ResponsavelId { get; set; }

    [Required]
    public string Tipo { get; set; } = "Entrega";

    [Required]
    public string Localizacao { get; set; } = "Escritório";

    [Required]
    public string Status { get; set; } = "Em uso";

    [StringLength(10000)]
    public string? Observacao { get; set; }
}

public class ConferenciaViewModel
{
    public string Situacao { get; set; } = "Conferido";

    [StringLength(10000)]
    public string? Observacao { get; set; }
}

public class NovoInventarioViewModel
{
    [Required]
    [StringLength(160)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = "";
}

public record FotoDisponivel(string Caminho, string TipoConteudo);

public record ArquivoGerado(byte[] Conteudo, string TipoConteudo, string Nome);
