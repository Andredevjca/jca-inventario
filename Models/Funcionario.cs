using System.ComponentModel.DataAnnotations;

namespace JcaInventario.Models;

public class Funcionario : Cadastro
{
    [EmailAddress]
    [StringLength(190)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(40)]
    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    [StringLength(60)]
    [Display(Name = "Matrícula")]
    public string? Matricula { get; set; }

    [StringLength(120)]
    [Display(Name = "Cargo")]
    public string? Cargo { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Setor")]
    public int SetorId { get; set; }

    [Display(Name = "Tipo de trabalho")]
    public string TipoTrabalho { get; set; } = "Presencial";
}
