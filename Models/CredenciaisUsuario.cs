namespace JcaInventario.Models;

public class CredenciaisUsuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public bool Administrador { get; set; }
}
