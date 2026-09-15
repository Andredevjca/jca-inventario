using Dapper;
using JcaInventario.Infraestrutura;
using JcaInventario.DTOs;
using JcaInventario.Modelos;
using JcaInventario.Repositorios;
using JcaInventario.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace JcaInventario.Controllers;

[ApiController, Authorize, Route("api")]
public class InventarioController(Banco banco, ServicoEquipamentos servico) : ControllerBase
{
    private int UsuarioAtual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("movimentacoes")]
    public async Task<object> Movimentacoes() { await using var conexao = await banco.AbrirAsync(); return await conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " ORDER BY m.Id DESC"); }
    [HttpGet("manutencoes")]
    public async Task<object> Manutencoes() { await using var conexao = await banco.AbrirAsync(); return await conexao.QueryAsync("SELECT m.*,e.NumeroPatrimonio,e.Modelo FROM manutencoes m JOIN equipamentos e ON e.Id=m.EquipamentoId ORDER BY m.Id DESC"); }
    [HttpPost("manutencoes")]
    public async Task<object> SalvarManutencao(Manutencao manutencao) => new { id = await servico.SalvarManutencaoAsync(manutencao, UsuarioAtual) };
    [HttpGet("inventarios")]
    public async Task<object> Inventarios()
    {
        await using var conexao = await banco.AbrirAsync();
        return await conexao.QueryAsync("""
            SELECT i.*, COUNT(c.Id) Total, COALESCE(SUM(c.Situacao='Conferido'),0) Conferidos,
            COALESCE(SUM(c.Situacao='Pendente'),0) Pendentes, COALESCE(SUM(c.Situacao='Divergência'),0) Divergencias
            FROM inventarios i LEFT JOIN conferencias c ON c.InventarioId=i.Id GROUP BY i.Id ORDER BY i.Id DESC
            """);
    }
    [HttpPost("inventarios")]
    public async Task<object> CriarInventario(NovoInventario inventario)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = await conexao.BeginTransactionAsync();
        var id = await conexao.ExecuteScalarAsync<int>("INSERT INTO inventarios (Nome,UsuarioId) VALUES (@Nome,@usuario); SELECT LAST_INSERT_ID()", new { inventario.Nome, usuario = UsuarioAtual }, transacao);
        await conexao.ExecuteAsync("INSERT INTO conferencias (InventarioId,EquipamentoId) SELECT @id,Id FROM equipamentos WHERE Status NOT IN ('Baixado','Inativo')", new { id }, transacao);
        await transacao.CommitAsync(); return new { id };
    }
    [HttpGet("inventarios/{id:int}")]
    public async Task<object> Conferencias(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var inventario = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id", new { id }) ?? throw new KeyNotFoundException();
        var itens = await conexao.QueryAsync("SELECT c.*,e.NumeroPatrimonio,e.Modelo,e.NumeroSerie,e.Localizacao,f.Nome Responsavel,u.Nome Usuario FROM conferencias c JOIN equipamentos e ON e.Id=c.EquipamentoId LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN usuarios u ON u.Id=c.UsuarioId WHERE InventarioId=@id ORDER BY e.NumeroPatrimonio,e.Id", new { id });
        return new { inventario, itens };
    }
    [HttpPost("inventarios/{id:int}/conferencias/{equipamentoId:int}")]
    public async Task<IActionResult> Conferir(int id, int equipamentoId, Conferencia conferencia)
    {
        if (!new[] { "Conferido", "Pendente", "Divergência" }.Contains(conferencia.Situacao)) throw new ArgumentException("Situação inválida.");
        if (conferencia.Situacao == "Divergência" && string.IsNullOrWhiteSpace(conferencia.Observacao)) throw new ArgumentException("Descreva a divergência encontrada.");
        await using var conexao = await banco.AbrirAsync(); await using var transacao = await conexao.BeginTransactionAsync();
        var inventario = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id FOR UPDATE", new { id }, transacao) ?? throw new KeyNotFoundException();
        if (Convert.ToBoolean(inventario.Encerrado)) throw new ArgumentException("O inventário já está encerrado.");
        var anterior = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM conferencias WHERE InventarioId=@id AND EquipamentoId=@equipamentoId FOR UPDATE", new { id, equipamentoId }, transacao) ?? throw new KeyNotFoundException();
        await conexao.ExecuteAsync("UPDATE conferencias SET Situacao=@Situacao,Observacao=@Observacao,Data=CURRENT_TIMESTAMP,UsuarioId=@usuario WHERE InventarioId=@id AND EquipamentoId=@equipamentoId", new { conferencia.Situacao, conferencia.Observacao, usuario = UsuarioAtual, id, equipamentoId }, transacao);
        await ServicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, equipamentoId, UsuarioAtual, $"Conferência: {inventario.Nome}", anterior, conferencia);
        await transacao.CommitAsync(); return NoContent();
    }
    [HttpPost("inventarios/{id:int}/encerrar")]
    public async Task<IActionResult> Encerrar(int id)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = await conexao.BeginTransactionAsync();
        var inventario = await conexao.QuerySingleOrDefaultAsync("SELECT * FROM inventarios WHERE Id=@id FOR UPDATE", new { id }, transacao) ?? throw new KeyNotFoundException();
        if (Convert.ToBoolean(inventario.Encerrado)) throw new ArgumentException("O inventário já está encerrado.");
        if (await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM conferencias WHERE InventarioId=@id AND Situacao='Pendente'", new { id }, transacao) > 0) throw new ArgumentException("Confira todos os equipamentos antes de encerrar.");
        await conexao.ExecuteAsync("UPDATE inventarios SET Encerrado=1 WHERE Id=@id", new { id }, transacao);
        await transacao.CommitAsync(); return NoContent();
    }
    [HttpGet("dashboard")]
    public async Task<object> Dashboard()
    {
        await using var conexao = await banco.AbrirAsync();
        var totais = await conexao.QuerySingleAsync("""
            SELECT COUNT(*) Total, COALESCE(SUM(Status='Em uso'),0) EmUso, COALESCE(SUM(Status='Em estoque'),0) EmEstoque,
            COALESCE(SUM(Status='Em manutenção'),0) EmManutencao, COALESCE(SUM(Localizacao='Home Office'),0) HomeOffice,
            COALESCE(SUM(Status='Baixado'),0) Baixados, COALESCE(SUM(ResponsavelId IS NULL),0) SemResponsavel,
            COALESCE(SUM(NumeroPatrimonio IS NULL),0) SemPatrimonio, COALESCE(SUM(NumeroSerie IS NULL),0) SemSerie FROM equipamentos
            """);
        var porSetor = await conexao.QueryAsync("SELECT COALESCE(s.Nome,'Sem setor') Nome,COUNT(*) Total FROM equipamentos e LEFT JOIN funcionarios f ON f.Id=e.ResponsavelId LEFT JOIN setores s ON s.Id=f.SetorId GROUP BY s.Nome ORDER BY Total DESC");
        var porLocalizacao = await conexao.QueryAsync("SELECT Localizacao Nome,COUNT(*) Total FROM equipamentos GROUP BY Localizacao ORDER BY Total DESC");
        var porStatus = await conexao.QueryAsync("SELECT Status Nome,COUNT(*) Total FROM equipamentos GROUP BY Status ORDER BY Total DESC");
        var pendentes = await conexao.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT c.EquipamentoId) FROM conferencias c JOIN inventarios i ON i.Id=c.InventarioId WHERE c.Situacao='Pendente' AND i.Encerrado=0");
        var recentes = await conexao.QueryAsync(RepositorioInventario.ConsultaMovimentacoes + " ORDER BY m.Id DESC LIMIT 8");
        return new { totais, porSetor, porLocalizacao, porStatus, pendentes, recentes };
    }
}
