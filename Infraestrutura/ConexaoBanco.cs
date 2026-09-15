using MySqlConnector;

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

    public async Task<MySqlConnection> AbrirAsync()
    {
        var conexao = new MySqlConnection(_stringConexao);
        await conexao.OpenAsync();
        return conexao;
    }
}
