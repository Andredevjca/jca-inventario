using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.Repositories;
using JcaInventario.ViewModels;
namespace JcaInventario.Servicos;

public class ServicoInventario(Banco banco, IRepositorioInventario repositorio, ServicoEquipamentos servicoEquipamentos) : IServicoInventario
{
    public async Task<byte[]> GerarPdfAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var inventario = await repositorio.ObterInventarioAsync(conexao, new { id }, transacao)
            ?? throw new KeyNotFoundException();
        var itens = (await repositorio.ListarRelatorioAsync(conexao, id, transacao)).ToList();
        var relatorio = new RelatorioInventario(id, (string)inventario.Nome, (DateTime)inventario.Data,
            Convert.ToBoolean((object)inventario.Encerrado), itens);
        await transacao.CommitAsync();
        return GeradorInventario.Gerar(relatorio);
    }
    public async Task<object> ListarMovimentacoesAsync() { await using var conexao = await banco.AbrirAsync(); return await repositorio.ListarMovimentacoesAsync(conexao); }
    public async Task<object> ListarManutencoesAsync() { await using var conexao = await banco.AbrirAsync(); return await repositorio.ListarManutencoesAsync(conexao); }
    public async Task<object> ListarInventariosAsync()
    {
        await using var conexao = await banco.AbrirAsync();
        return await repositorio.ListarInventariosAsync(conexao);
    }
    public async Task<object> CriarInventarioAsync(NovoInventarioViewModel inventario, int usuario)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
        var id = await repositorio.InserirInventarioAsync(conexao, new { inventario.Nome, usuario = usuario }, transacao);
        await repositorio.InserirConferenciasAsync(conexao, new { id }, transacao);
        await transacao.CommitAsync(); return new { id };
    }
    public async Task<object> ObterConferenciasAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var inventario = await repositorio.ObterInventarioAsync(conexao, new { id }) ?? throw new KeyNotFoundException();
        var itens = await repositorio.ListarConferenciasAsync(conexao, new { id });
        return new { inventario, itens };
    }
    public async Task ConferirAsync(int id, int equipamentoId, ConferenciaViewModel conferencia, int usuario)
    {
        if (!new[] { "Conferido", "Pendente", "Divergência" }.Contains(conferencia.Situacao)) throw new ArgumentException("Situação inválida.");
        if (conferencia.Situacao == "Divergência" && string.IsNullOrWhiteSpace(conferencia.Observacao)) throw new ArgumentException("Descreva a divergência encontrada.");
        await using var conexao = await banco.AbrirAsync(); await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
        var inventario = await repositorio.ObterInventarioParaAtualizacaoAsync(conexao, new { id }, transacao) ?? throw new KeyNotFoundException();
        if (Convert.ToBoolean(inventario.Encerrado)) throw new ArgumentException("O inventário já está encerrado.");
        var anterior = await repositorio.ObterConferenciaParaAtualizacaoAsync(conexao, new { id, equipamentoId }, transacao) ?? throw new KeyNotFoundException();
        await repositorio.AtualizarConferenciaAsync(conexao, new { conferencia.Situacao, conferencia.Observacao, usuario = usuario, id, equipamentoId }, transacao);
        await servicoEquipamentos.RegistrarHistoricoAsync(conexao, transacao, equipamentoId, usuario, $"Conferência: {inventario.Nome}", anterior, conferencia);
        await transacao.CommitAsync();
    }
    public async Task EncerrarAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync(); await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
        var inventario = await repositorio.ObterInventarioParaAtualizacaoAsync(conexao, new { id }, transacao) ?? throw new KeyNotFoundException();
        if (Convert.ToBoolean(inventario.Encerrado)) throw new ArgumentException("O inventário já está encerrado.");
        if (await repositorio.ContarPendentesAsync(conexao, new { id }, transacao) > 0) throw new ArgumentException("Confira todos os equipamentos antes de encerrar.");
        await repositorio.EncerrarInventarioAsync(conexao, new { id }, transacao);
        await transacao.CommitAsync();
    }
    public async Task<object> ObterDashboardAsync()
    {
        await using var conexao = await banco.AbrirAsync();
        var totais = await repositorio.ObterTotaisAsync(conexao);
        var porSetor = await repositorio.AgruparPorSetorAsync(conexao);
        var porLocalizacao = await repositorio.AgruparPorLocalizacaoAsync(conexao);
        var porStatus = await repositorio.AgruparPorStatusAsync(conexao);
        var pendentes = await repositorio.ContarEquipamentosPendentesAsync(conexao);
        var recentes = await repositorio.ListarMovimentacoesRecentesAsync(conexao);
        return new { totais, porSetor, porLocalizacao, porStatus, pendentes, recentes };
    }
}
