using Dapper;
using JcaInventario.Infraestrutura;
namespace JcaInventario.Repositorios;

public class RepositorioInventario(Banco banco)
{
    public const string ConsultaEquipamentos = """
        SELECT e.*, t.Nome Tipo, f.Nome Responsavel, f.SetorId, s.Nome Setor
        FROM equipamentos e JOIN tipos_equipamento t ON t.Id=e.TipoId
        LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId
        """;
    public const string ConsultaMovimentacoes = """
        SELECT m.*, e.NumeroPatrimonio, e.Modelo, a.Nome ResponsavelAnterior, n.Nome NovoResponsavel,
        sa.Nome SetorAnterior, sn.Nome NovoSetor, u.Nome Usuario FROM movimentacoes m
        JOIN equipamentos e ON e.Id=m.EquipamentoId JOIN usuarios u ON u.Id=m.UsuarioId
        LEFT JOIN funcionarios a ON a.Id=m.ResponsavelAnteriorId LEFT JOIN funcionarios n ON n.Id=m.NovoResponsavelId
        LEFT JOIN setores sa ON sa.Id=m.SetorAnteriorId LEFT JOIN setores sn ON sn.Id=m.NovoSetorId
        """;
    public async Task<IEnumerable<dynamic>> EquipamentosAsync()
    {
        await using var conexao = await banco.AbrirAsync();
        return await conexao.QueryAsync(ConsultaEquipamentos + " ORDER BY e.Id DESC");
    }
    public async Task<object> DetalhesAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var equipamento = await conexao.QuerySingleOrDefaultAsync(ConsultaEquipamentos + " WHERE e.Id=@id", new { id }) ?? throw new KeyNotFoundException();
        var movimentacoes = await conexao.QueryAsync(ConsultaMovimentacoes + " WHERE m.EquipamentoId=@id ORDER BY m.Id DESC", new { id });
        var manutencoes = await conexao.QueryAsync("SELECT * FROM manutencoes WHERE EquipamentoId=@id ORDER BY Id DESC", new { id });
        var historico = await conexao.QueryAsync("SELECT h.*,u.Nome Usuario FROM historico h JOIN usuarios u ON u.Id=h.UsuarioId WHERE EquipamentoId=@id ORDER BY h.Id DESC", new { id });
        return new { equipamento, movimentacoes, manutencoes, historico };
    }
}
