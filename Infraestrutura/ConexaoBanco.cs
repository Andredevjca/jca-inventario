using Microsoft.Data.SqlClient;

namespace JcaInventario.Infraestrutura;

public class ConexaoBanco
{
    private readonly string _stringConexao;

    public ConexaoBanco(IConfiguration configuracao)
    {
        _stringConexao = configuracao.GetConnectionString("Banco")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Banco.");
    }

    public string Conexao => _stringConexao;

    public async Task<SqlConnection> AbrirAsync()
    {
        var conexao = new SqlConnection(_stringConexao);
        await conexao.OpenAsync();
        return conexao;
    }
}
