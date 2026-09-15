using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Equipamento
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Tipo")]
    public int TipoId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Marca")]
    public string Marca { get; set; } = "";

    [Required]
    [StringLength(160)]
    [Display(Name = "Modelo")]
    public string Modelo { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Patrimônio")]
    public string? NumeroPatrimonio { get; set; }

    [StringLength(150)]
    [Display(Name = "Número de série")]
    public string? NumeroSerie { get; set; }

    [Display(Name = "Data de aquisição")]
    public DateTime? DataAquisicao { get; set; }

    [Range(0, 999999999999.99)]
    [Display(Name = "Valor de aquisição")]
    public decimal ValorAquisicao { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Em estoque";

    [Display(Name = "Localização")]
    public string Localizacao { get; set; } = "Estoque";

    [Display(Name = "Responsável")]
    public int? ResponsavelId { get; set; }

    [StringLength(10000)]
    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    [Display(Name = "Processador")]
    public string? Processador { get; set; }

    [Display(Name = "Memória RAM")]
    public string? MemoriaRam { get; set; }

    [Display(Name = "Armazenamento")]
    public string? Armazenamento { get; set; }

    [Display(Name = "Sistema operacional")]
    public string? SistemaOperacional { get; set; }

    public string? Foto { get; set; }
}
