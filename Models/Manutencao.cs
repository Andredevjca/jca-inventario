using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Manutencao
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Equipamento")]
    public int EquipamentoId { get; set; }

    [Display(Name = "Data de entrada")]
    public DateTime DataEntrada { get; set; } = DateTime.Today;

    [Display(Name = "Data de saída")]
    public DateTime? DataSaida { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = "";

    [Required]
    [StringLength(10000)]
    [Display(Name = "Problema")]
    public string Problema { get; set; } = "";

    [StringLength(10000)]
    [Display(Name = "Solução")]
    public string? Solucao { get; set; }

    [Range(0, 999999999999.99)]
    [Display(Name = "Valor")]
    public decimal Valor { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Responsável")]
    public string Responsavel { get; set; } = "";

    [StringLength(10000)]
    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }
}
