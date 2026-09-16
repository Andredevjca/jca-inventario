using Dapper;
using JcaInventario.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace JcaInventario.Repositories;

public class RepositorioInventario : IRepositorioInventario
{
    public Task<IEnumerable<JcaInventario.Models.ItemRelatorioInventario>> ListarRelatorioAsync(SqlConnection conexao, int id, SqlTransaction transacao)
        => conexao.QueryAsync<JcaInventario.Models.ItemRelatorioInventario>("""
            SELECT c.EquipamentoId,c.Situacao,c.Observacao,c.Data,u.Nome Usuario,e.*
            FROM conferencias c JOIN inventario_equipamentos e ON e.ConferenciaId=c.Id
            LEFT JOIN usuarios u ON u.Id=c.UsuarioId
            WHERE c.InventarioId=@id ORDER BY e.Setor,e.NumeroPatrimonio,c.EquipamentoId
            """, new { id }, transacao);
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

    public Task<IEnumerable<dynamic>> ListarMovimentacoesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync(ConsultaMovimentacoes + " ORDER BY m.Id DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarManutencoesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT m.*,e.NumeroPatrimonio,e.Modelo FROM manutencoes m JOIN equipamentos e ON e.Id=m.EquipamentoId ORDER BY m.Id DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarInventariosAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("""
            SELECT i.*, COUNT(c.Id) Total, COALESCE(SUM(CASE WHEN c.Situacao='Conferido' THEN 1 ELSE 0 END),0) Conferidos,
            COALESCE(SUM(CASE WHEN c.Situacao='Pendente' THEN 1 ELSE 0 END),0) Pendentes, COALESCE(SUM(CASE WHEN c.Situacao='Divergência' THEN 1 ELSE 0 END),0) Divergencias
            FROM inventarios i LEFT JOIN conferencias c ON c.InventarioId=i.Id GROUP BY i.Id,i.Nome,i.Data,i.Encerrado,i.UsuarioId ORDER BY i.Id DESC
            """, parametros, transacao);

    public Task<int> InserirInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("INSERT INTO inventarios (Nome,UsuarioId) VALUES (@Nome,@usuario); SELECT CAST(SCOPE_IDENTITY() AS int)", parametros, transacao);

    public Task<int> InserirConferenciasAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("""
            INSERT INTO conferencias (InventarioId,EquipamentoId)
            SELECT @id,Id FROM equipamentos WHERE Status NOT IN ('Baixado','Inativo');
            INSERT INTO inventario_equipamentos
              (ConferenciaId,NumeroPatrimonio,NumeroSerie,Tipo,Marca,Modelo,Responsavel,Setor,Localizacao,Status,CapturadoNaCriacao)
            SELECT c.Id,e.NumeroPatrimonio,e.NumeroSerie,t.Nome,e.Marca,e.Modelo,f.Nome,s.Nome,e.Localizacao,e.Status,1
            FROM conferencias c JOIN equipamentos e ON e.Id=c.EquipamentoId
            JOIN tipos_equipamento t ON t.Id=e.TipoId
            LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId
            WHERE c.InventarioId=@id;
            """, parametros, transacao);

    public Task<dynamic> ObterInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarConferenciasAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("""
            SELECT c.*,e.NumeroPatrimonio,e.Marca,e.Modelo,e.NumeroSerie,e.Tipo,e.Localizacao,e.Status,
              e.Responsavel,e.Setor,e.CapturadoEm,e.CapturadoNaCriacao,u.Nome Usuario
            FROM conferencias c JOIN inventario_equipamentos e ON e.ConferenciaId=c.Id
            LEFT JOIN usuarios u ON u.Id=c.UsuarioId
            WHERE c.InventarioId=@id ORDER BY e.Setor,e.NumeroPatrimonio,c.EquipamentoId
            """, parametros, transacao);

    public Task<dynamic> ObterInventarioParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WITH (UPDLOCK, HOLDLOCK) WHERE Id=@id", parametros, transacao);

    public Task<dynamic> ObterConferenciaParaAtualizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleOrDefaultAsync("SELECT * FROM conferencias WITH (UPDLOCK, HOLDLOCK) WHERE InventarioId=@id AND EquipamentoId=@equipamentoId", parametros, transacao);

    public Task<int> AtualizarConferenciaAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE conferencias SET Situacao=@Situacao,Observacao=@Observacao,Data=CURRENT_TIMESTAMP,UsuarioId=@usuario WHERE InventarioId=@id AND EquipamentoId=@equipamentoId", parametros, transacao);

    public Task<int> ContarPendentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM conferencias WHERE InventarioId=@id AND Situacao='Pendente'", parametros, transacao);

    public Task<int> EncerrarInventarioAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteAsync("UPDATE inventarios SET Encerrado=1 WHERE Id=@id", parametros, transacao);

    public Task<dynamic> ObterTotaisAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QuerySingleAsync("""
            SELECT COUNT(*) Total, COALESCE(SUM(CASE WHEN Status='Em uso' THEN 1 ELSE 0 END),0) EmUso, COALESCE(SUM(CASE WHEN Status='Em estoque' THEN 1 ELSE 0 END),0) EmEstoque,
            COALESCE(SUM(CASE WHEN Status='Em manutenção' THEN 1 ELSE 0 END),0) EmManutencao, COALESCE(SUM(CASE WHEN Localizacao='Home Office' THEN 1 ELSE 0 END),0) HomeOffice,
            COALESCE(SUM(CASE WHEN Status='Baixado' THEN 1 ELSE 0 END),0) Baixados, COALESCE(SUM(CASE WHEN ResponsavelId IS NULL THEN 1 ELSE 0 END),0) SemResponsavel,
            COALESCE(SUM(CASE WHEN NumeroPatrimonio IS NULL THEN 1 ELSE 0 END),0) SemPatrimonio, COALESCE(SUM(CASE WHEN NumeroSerie IS NULL THEN 1 ELSE 0 END),0) SemSerie FROM equipamentos
            """, parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorSetorAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT COALESCE(s.Nome,'Sem setor') Nome,COUNT(*) Total FROM equipamentos e LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId GROUP BY s.Nome ORDER BY Total DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorLocalizacaoAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT Localizacao Nome,COUNT(*) Total FROM equipamentos GROUP BY Localizacao ORDER BY Total DESC", parametros, transacao);

    public Task<IEnumerable<dynamic>> AgruparPorStatusAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync("SELECT Status Nome,COUNT(*) Total FROM equipamentos GROUP BY Status ORDER BY Total DESC", parametros, transacao);

    public Task<int> ContarEquipamentosPendentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT c.EquipamentoId) FROM conferencias c JOIN inventarios i ON i.Id=c.InventarioId WHERE c.Situacao='Pendente' AND i.Encerrado=0", parametros, transacao);

    public Task<IEnumerable<dynamic>> ListarMovimentacoesRecentesAsync(SqlConnection conexao, object? parametros = null, SqlTransaction? transacao = null)
        => conexao.QueryAsync(ConsultaMovimentacoes.Replace("SELECT m.*", "SELECT TOP (8) m.*") + " ORDER BY m.Id DESC", parametros, transacao);
}
