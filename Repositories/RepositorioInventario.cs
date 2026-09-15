using Dapper;
using JcaInventario.Interfaces.Repositories;
using MySqlConnector;

namespace JcaInventario.Repositories;

public class RepositorioInventario : IRepositorioInventario
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

    public Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync(ConsultaMovimentacoes + " ORDER BY m.Id DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarManutencoesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT m.*,e.NumeroPatrimonio,e.Modelo FROM manutencoes m JOIN equipamentos e ON e.Id=m.EquipamentoId ORDER BY m.Id DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarInventariosAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("""
            SELECT i.*, COUNT(c.Id) Total, COALESCE(SUM(c.Situacao='Conferido'),0) Conferidos,
            COALESCE(SUM(c.Situacao='Pendente'),0) Pendentes, COALESCE(SUM(c.Situacao='Divergência'),0) Divergencias
            FROM inventarios i LEFT JOIN conferencias c ON c.InventarioId=i.Id GROUP BY i.Id ORDER BY i.Id DESC
            """, parametros, transacao);

    public Task<int> InserirInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO inventarios (Nome,UsuarioId) VALUES (@Nome,@usuario); SELECT LAST_INSERT_ID()", parametros, transacao);

    public Task<int> InserirConferenciasAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("INSERT INTO conferencias (InventarioId,EquipamentoId) SELECT @id,Id FROM equipamentos WHERE Status NOT IN ('Baixado','Inativo')", parametros, transacao);

    public Task<dynamic> ObterInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarConferenciasAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT c.*,e.NumeroPatrimonio,e.Modelo,e.NumeroSerie,e.Localizacao,f.Nome Responsavel,u.Nome Usuario FROM conferencias c JOIN equipamentos e ON e.Id=c.EquipamentoId LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN usuarios u ON u.Id=c.UsuarioId WHERE InventarioId=@id ORDER BY e.NumeroPatrimonio,e.Id", parametros, transacao);

    public Task<dynamic> ObterInventarioParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id FOR UPDATE", parametros, transacao);

    public Task<dynamic> ObterConferenciaParaAtualizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM conferencias WHERE InventarioId=@id AND EquipamentoId=@equipamentoId FOR UPDATE", parametros, transacao);

    public Task<int> AtualizarConferenciaAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE conferencias SET Situacao=@Situacao,Observacao=@Observacao,Data=CURRENT_TIMESTAMP,UsuarioId=@usuario WHERE InventarioId=@id AND EquipamentoId=@equipamentoId", parametros, transacao);

    public Task<int> ContarPendentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM conferencias WHERE InventarioId=@id AND Situacao='Pendente'", parametros, transacao);

    public Task<int> EncerrarInventarioAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE inventarios SET Encerrado=1 WHERE Id=@id", parametros, transacao);

    public Task<dynamic> ObterTotaisAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QuerySingleAsync("""
            SELECT COUNT(*) Total, COALESCE(SUM(Status='Em uso'),0) EmUso, COALESCE(SUM(Status='Em estoque'),0) EmEstoque,
            COALESCE(SUM(Status='Em manutenção'),0) EmManutencao, COALESCE(SUM(Localizacao='Home Office'),0) HomeOffice,
            COALESCE(SUM(Status='Baixado'),0) Baixados, COALESCE(SUM(ResponsavelId IS NULL),0) SemResponsavel,
            COALESCE(SUM(NumeroPatrimonio IS NULL),0) SemPatrimonio, COALESCE(SUM(NumeroSerie IS NULL),0) SemSerie FROM equipamentos
            """, parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorSetorAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT COALESCE(s.Nome,'Sem setor') Nome,COUNT(*) Total FROM equipamentos e LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId GROUP BY s.Nome ORDER BY Total DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorLocalizacaoAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT Localizacao Nome,COUNT(*) Total FROM equipamentos GROUP BY Localizacao ORDER BY Total DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorStatusAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT Status Nome,COUNT(*) Total FROM equipamentos GROUP BY Status ORDER BY Total DESC", parametros, transacao);

    public Task<int> ContarEquipamentosPendentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT c.EquipamentoId) FROM conferencias c JOIN inventarios i ON i.Id=c.InventarioId WHERE c.Situacao='Pendente' AND i.Encerrado=0", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarMovimentacoesRecentesAsync(MySqlConnection conexao, object? parametros = null, MySqlTransaction? transacao = null)
        => conexao.QueryAsync(ConsultaMovimentacoes + " ORDER BY m.Id DESC LIMIT 8", parametros, transacao);
}
