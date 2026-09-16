using Microsoft.Data.SqlClient;
using Dapper;
using System.Security.Claims;
namespace JcaInventario.Infraestrutura;

public sealed class Banco(IConfiguration configuracao, IHttpContextAccessor contexto)
{
    public string Conexao => configuracao.GetConnectionString("Banco") ?? throw new InvalidOperationException("Configure ConnectionStrings:Banco.");
    public async Task<SqlConnection> AbrirAsync()
    {
        var conexao = new SqlConnection(Conexao);
        try
        {
            await conexao.OpenAsync();
            var usuario = contexto.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? usuarioId = int.TryParse(usuario, out var id) ? id : null;
            await conexao.ExecuteAsync("EXEC sys.sp_set_session_context @key=N'UsuarioId', @value=@usuarioId", new { usuarioId });
            return conexao;
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }
}
