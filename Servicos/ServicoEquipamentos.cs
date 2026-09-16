using JcaInventario.Infraestrutura;
using JcaInventario.Interfaces.Repositories;
using JcaInventario.Interfaces.Services;
using JcaInventario.Models;
using JcaInventario.Configuracoes;
using JcaInventario.ViewModels;
using Microsoft.Data.SqlClient;
using System.Text.Json;
namespace JcaInventario.Servicos;

public class ServicoEquipamentos(Banco banco, IRepositorioEquipamentos repositorio) : IServicoEquipamentos
{
    public async Task RegistrarHistoricoAsync(SqlConnection conexao, SqlTransaction transacao, int id, int usuario, string descricao, object? anterior, object? novo)
        => await repositorio.InserirHistoricoAsync(conexao, new { id, usuario, descricao, antes = anterior == null ? null : JsonSerializer.Serialize(anterior), depois = novo == null ? null : JsonSerializer.Serialize(novo) }, transacao);

    private async Task ValidarAsync(SqlConnection conexao, SqlTransaction transacao, Equipamento equipamento)
    {
        if (!OpcoesInventario.Status.Contains(equipamento.Status) || !OpcoesInventario.Localizacoes.Contains(equipamento.Localizacao)) throw new ArgumentException("Status ou localização inválidos.");
        if (equipamento.ValorAquisicao < 0) throw new ArgumentException("O valor não pode ser negativo.");
        if (equipamento.ResponsavelId.HasValue && !await repositorio.ResponsavelAtivoAsync(conexao, new { Id = equipamento.ResponsavelId }, transacao)) throw new ArgumentException("Selecione um funcionário ativo em um setor ativo.");
        if (!await repositorio.TipoAtivoAsync(conexao, equipamento, transacao)) throw new ArgumentException("Selecione um tipo de equipamento ativo.");
        if (equipamento.Status is "Em uso" or "Home Office" && equipamento.ResponsavelId == null) throw new ArgumentException("Equipamentos em uso precisam de um responsável.");
        if (equipamento.Status is "Disponível" or "Em estoque" or "Baixado" or "Inativo" && equipamento.ResponsavelId != null) throw new ArgumentException("Retire o responsável antes de disponibilizar, inativar ou baixar o equipamento.");
        equipamento.NumeroPatrimonio = string.IsNullOrWhiteSpace(equipamento.NumeroPatrimonio) ? null : equipamento.NumeroPatrimonio.Trim();
        equipamento.NumeroSerie = string.IsNullOrWhiteSpace(equipamento.NumeroSerie) ? null : equipamento.NumeroSerie.Trim();
    }

    public async Task RegistrarMovimentacaoAsync(SqlConnection conexao, SqlTransaction transacao, Equipamento? anterior, Equipamento novo, int usuario, string tipo, string? observacao)
    {
        var setorAnterior = anterior?.ResponsavelId == null ? (int?)null : await repositorio.ObterSetorResponsavelAsync(conexao, new { Id = anterior.ResponsavelId }, transacao);
        var novoSetor = novo.ResponsavelId == null ? (int?)null : await repositorio.ObterSetorResponsavelAsync(conexao, new { Id = novo.ResponsavelId }, transacao);
        await repositorio.InserirMovimentacaoAsync(conexao, new { novo.Id, anteriorId = anterior?.ResponsavelId, novoId = novo.ResponsavelId, setorAnterior, novoSetor, localAnterior = anterior?.Localizacao, localNovo = novo.Localizacao, statusAnterior = anterior?.Status, statusNovo = novo.Status, tipo, usuario, observacao }, transacao);
    }

    public async Task<int> SalvarAsync(Equipamento equipamento, int usuario)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
        var anterior = equipamento.Id == 0 ? null : await repositorio.ObterParaAtualizacaoAsync(conexao, equipamento, transacao) ?? throw new KeyNotFoundException();
        await ValidarAsync(conexao, transacao, equipamento);
        if (equipamento.Status == "Em manutenção" && anterior?.Status != "Em manutenção") throw new ArgumentException("Utilize o cadastro de manutenção para enviar o equipamento.");
        if (anterior?.Status == "Em manutenção" && (equipamento.Status != anterior.Status || equipamento.ResponsavelId != anterior.ResponsavelId || equipamento.Localizacao != anterior.Localizacao)) throw new ArgumentException("Finalize a manutenção antes de movimentar o equipamento.");
        equipamento.Foto = anterior?.Foto;
        if (anterior == null)
        {
            equipamento.Id = await repositorio.InserirAsync(conexao, equipamento, transacao);
        }
        else await repositorio.AtualizarAsync(conexao, equipamento, transacao);
        if (anterior == null || anterior.ResponsavelId != equipamento.ResponsavelId || anterior.Localizacao != equipamento.Localizacao || anterior.Status != equipamento.Status)
            await RegistrarMovimentacaoAsync(conexao, transacao, anterior, equipamento, usuario, anterior == null ? "Cadastro" : anterior.ResponsavelId != equipamento.ResponsavelId ? (equipamento.ResponsavelId == null ? "Devolução" : anterior.ResponsavelId == null ? "Entrega" : "Transferência") : "Alteração de localização/status", equipamento.Observacoes);
        await RegistrarHistoricoAsync(conexao, transacao, equipamento.Id, usuario, anterior == null ? "Equipamento cadastrado" : "Cadastro atualizado", anterior, equipamento);
        await transacao.CommitAsync();
        return equipamento.Id;
    }

    public async Task MovimentarAsync(int id, MovimentacaoViewModel movimento, int usuario)
    {
        if (!OpcoesInventario.Movimentacoes.Contains(movimento.Tipo)) throw new ArgumentException("Tipo de movimentação inválido.");
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync();
        var anterior = await repositorio.ObterParaMovimentacaoAsync(conexao, new { id }, transacao) ?? throw new KeyNotFoundException();
        var novo = JsonSerializer.Deserialize<Equipamento>(JsonSerializer.Serialize(anterior))!;
        if (anterior.Status == "Em manutenção" || movimento.Status == "Em manutenção") throw new ArgumentException("Envie e retorne pela tela de manutenções.");
        novo.ResponsavelId = movimento.ResponsavelId; novo.Localizacao = movimento.Localizacao; novo.Status = movimento.Status;
        if (movimento.Tipo == "Entrega" && (anterior.ResponsavelId != null || novo.ResponsavelId == null)) throw new ArgumentException("Entrega exige equipamento sem responsável e um novo responsável.");
        if (movimento.Tipo == "Transferência" && (anterior.ResponsavelId == null || novo.ResponsavelId == null || anterior.ResponsavelId == novo.ResponsavelId)) throw new ArgumentException("Selecione um responsável diferente para transferir.");
        if (movimento.Tipo == "Devolução" && (anterior.ResponsavelId == null || novo.ResponsavelId != null)) throw new ArgumentException("Devolução deve retirar o responsável atual.");
        if (movimento.Tipo == "Transferência para estoque" && (novo.Localizacao != "Estoque" || novo.Status != "Em estoque" || novo.ResponsavelId != null)) throw new ArgumentException("Transferência para estoque exige status Em estoque, localização Estoque e nenhum responsável.");
        if (movimento.Tipo == "Alteração de localização" && novo.ResponsavelId != anterior.ResponsavelId) throw new ArgumentException("Use entrega, devolução ou transferência para alterar o responsável.");
        await ValidarAsync(conexao, transacao, novo);
        await repositorio.AtualizarSituacaoAsync(conexao, novo, transacao);
        await RegistrarMovimentacaoAsync(conexao, transacao, anterior, novo, usuario, movimento.Tipo, movimento.Observacao);
        await RegistrarHistoricoAsync(conexao, transacao, id, usuario, movimento.Tipo, anterior, novo);
        await transacao.CommitAsync();
    }

    public async Task<object> ListarAsync()
    {
        await using var conexao = await banco.AbrirAsync();
        return await repositorio.ListarAsync(conexao);
    }

    public async Task<object> ObterDetalhesAsync(int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var equipamento = await repositorio.ObterDetalhesAsync(conexao, id) ?? throw new KeyNotFoundException();
        var movimentacoes = await repositorio.ListarMovimentacoesAsync(conexao, id);
        var manutencoes = await repositorio.ListarManutencoesAsync(conexao, id);
        var historico = await repositorio.ListarHistoricoAsync(conexao, id);
        return new { equipamento, movimentacoes, manutencoes, historico };
    }
}
