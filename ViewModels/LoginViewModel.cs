using System.ComponentModel.DataAnnotations;

namespace JcaInventario.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = "";

    [Display(Name = "Lembrar-me")]
    public bool Lembrar { get; set; }
}
