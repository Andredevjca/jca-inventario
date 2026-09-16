using MySqlConnector;
using Dapper;
using System.Security.Claims;
namespace JcaInventario.Infraestrutura;

public sealed class Banco(IConfiguration configuracao, IHttpContextAccessor contexto)
{
    public string Conexao => new MySqlConnectionStringBuilder(configuracao.GetConnectionString("Banco")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:Banco.")) { AllowUserVariables = true }.ConnectionString;
    public async Task<MySqlConnection> AbrirAsync()
    {
        var conexao = new MySqlConnection(Conexao);
        try
        {
            await conexao.OpenAsync();
            var usuario = contexto.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? usuarioId = int.TryParse(usuario, out var id) ? id : null;
            await conexao.ExecuteAsync("SET @jca_usuario_id = @usuarioId", new { usuarioId });
            return conexao;
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }
}
