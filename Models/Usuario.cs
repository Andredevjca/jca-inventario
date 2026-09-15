using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Usuario : Cadastro
{
    [Required]
    [EmailAddress]
    [StringLength(190)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [StringLength(128, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string? Senha { get; set; }

    [Display(Name = "Administrador")]
    public bool Administrador { get; set; }
}
