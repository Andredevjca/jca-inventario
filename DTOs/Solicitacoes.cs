using System.ComponentModel.DataAnnotations;
namespace JcaInventario.DTOs;
public class Entrada
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(128)] public string Senha { get; set; } = "";
    public bool Lembrar { get; set; }
}
public class Movimentacao
{
    public int? ResponsavelId { get; set; }
    [Required] public string Tipo { get; set; } = "Entrega";
    [Required] public string Localizacao { get; set; } = "Escritório";
    [Required] public string Status { get; set; } = "Em uso";
    [StringLength(10000)] public string? Observacao { get; set; }
}
public class Conferencia
{
    public string Situacao { get; set; } = "Conferido";
    [StringLength(10000)] public string? Observacao { get; set; }
}
public class NovoInventario
{
    [Required, StringLength(160), Display(Name = "Nome")] public string Nome { get; set; } = "";
}
