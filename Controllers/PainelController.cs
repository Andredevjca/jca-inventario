using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JcaInventario.Controllers;

public abstract class PainelController : Controller
{
    protected int UsuarioAtual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
