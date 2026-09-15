using MySqlConnector;
namespace JcaInventario.Infraestrutura;

public sealed class Banco(IConfiguration configuracao)
{
    public string Conexao => configuracao.GetConnectionString("Banco") ?? throw new InvalidOperationException("Configure ConnectionStrings:Banco.");
    public async Task<MySqlConnection> AbrirAsync()
    {
        var conexao = new MySqlConnection(Conexao);
        await conexao.OpenAsync();
        return conexao;
    }
}
