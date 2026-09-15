using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Cadastro
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = "";

    [Display(Name = "Ativo")]
    public bool Ativo { get; set; } = true;

    [StringLength(10000)]
    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }
}
