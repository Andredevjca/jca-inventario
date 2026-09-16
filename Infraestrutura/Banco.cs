using Microsoft.Data.SqlClient;
namespace JcaInventario.Infraestrutura;

public sealed class Banco(IConfiguration configuracao)
{
    public string Conexao => configuracao.GetConnectionString("Banco") ?? throw new InvalidOperationException("Configure ConnectionStrings:Banco.");
    public async Task<SqlConnection> AbrirAsync()
    {
        var conexao = new SqlConnection(Conexao);
        await conexao.OpenAsync();
        return conexao;
    }
}
